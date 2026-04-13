using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using SorumlulukHesaplama.Models;

namespace SorumlulukHesaplama.Services;

public static class PdfExchangeRateParser
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    public const int MaxPageCount = 10;
    public const int MaxTextLength = 500_000;

    /// <summary>
    /// Parse TCMB exchange rate PDF and extract date, EUR/USD, SDR/USD rates.
    /// Uses line-based label matching for reliable extraction.
    /// </summary>
    public static ExchangeData Parse(string filePath)
    {
        // Guardrail: file size
        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length > MaxFileSizeBytes)
            throw new InvalidOperationException(
                $"Dosya boyutu çok büyük ({fileInfo.Length / (1024 * 1024):F1} MB). Maksimum {MaxFileSizeBytes / (1024 * 1024)} MB desteklenmektedir.");

        var pageTexts = ExtractPageTexts(filePath);
        var fullText = string.Join("\n", pageTexts);

        Debug.WriteLine($"[PdfParser] Full text length: {fullText.Length}");

        // Extract date
        var dateMatch = Regex.Match(fullText, @"(\d{2}\.\d{2}\.\d{4})\s*Günü\s*Saat", RegexOptions.IgnoreCase);
        if (!dateMatch.Success)
            dateMatch = Regex.Match(fullText, @"(\d{2}\.\d{2}\.\d{4})");
        if (!dateMatch.Success)
            throw new InvalidOperationException("Tarih bulunamadı. Lütfen TCMB döviz kuru belgesini yükleyin.");

        var date = dateMatch.Groups[1].Value;
        Debug.WriteLine($"[PdfParser] Found date: {date}");

        double eurUsdRate = 0;
        double sdrUsdRate = 0;

        // Split into lines for label-based extraction
        var lines = fullText.Split('\n', '\r')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToArray();

        // Strategy 1: Line-based label matching
        foreach (var line in lines)
        {
            // EUR/USD cross rate: look for lines containing EUR label
            if (eurUsdRate == 0 && IsEurLine(line))
            {
                var rate = ExtractCrossRate(line, 0.9, 1.5);
                if (rate > 0)
                {
                    eurUsdRate = rate;
                    Debug.WriteLine($"[PdfParser] EUR/USD from line: {eurUsdRate} | line: {line}");
                }
            }

            // SDR/USD rate: look for lines containing SDR/XDR label
            if (sdrUsdRate == 0 && IsSdrLine(line))
            {
                var rate = ExtractCrossRate(line, 1.2, 1.6);
                if (rate > 0)
                {
                    sdrUsdRate = rate;
                    Debug.WriteLine($"[PdfParser] SDR/USD from line: {sdrUsdRate} | line: {line}");
                }
            }

            // Early exit: stop once both rates are found
            if (eurUsdRate > 0 && sdrUsdRate > 0)
                break;
        }

        // Strategy 2: Token-based fallback (original approach) if line-based failed
        if (eurUsdRate == 0 || sdrUsdRate == 0)
        {
            var tokens = Regex.Split(fullText, @"[\s\t\n\r]+")
                .Where(t => t.Length > 0)
                .ToArray();

            if (eurUsdRate == 0)
                eurUsdRate = FindRateByToken(tokens, ["EUR", "EURO"], 0.9, 1.5, 5);
            if (sdrUsdRate == 0)
                sdrUsdRate = FindRateByToken(tokens, ["SDR", "XDR"], 1.2, 1.6, 15);
        }

        Debug.WriteLine($"[PdfParser] Final values - EUR/USD: {eurUsdRate} SDR/USD: {sdrUsdRate}");

        if (eurUsdRate == 0)
            throw new InvalidOperationException("EUR/USD çapraz kuru bulunamadı. Bu belge çapraz kur içermiyor olabilir.");
        if (sdrUsdRate == 0)
            throw new InvalidOperationException("SDR/USD kuru bulunamadı. Lütfen belgeyi kontrol edin.");

        // Sanity check extracted rates
        if (eurUsdRate < 0.5 || eurUsdRate > 2.0)
            throw new InvalidOperationException($"EUR/USD kuru ({eurUsdRate:F4}) beklenen aralıkta değil. Lütfen belgeyi kontrol edin.");
        if (sdrUsdRate < 1.0 || sdrUsdRate > 2.0)
            throw new InvalidOperationException($"SDR/USD kuru ({sdrUsdRate:F4}) beklenen aralıkta değil. Lütfen belgeyi kontrol edin.");

        return new ExchangeData
        {
            Date = date,
            EurUsdRate = eurUsdRate,
            SdrUsdRate = sdrUsdRate
        };
    }

    private static bool IsEurLine(string line)
    {
        var upper = line.ToUpperInvariant();
        return upper.Contains("EUR") && !upper.Contains("SDR") && !upper.Contains("XDR");
    }

    private static bool IsSdrLine(string line)
    {
        var upper = line.ToUpperInvariant();
        return upper.Contains("SDR") || upper.Contains("XDR") || upper.Contains("ÖZEL ÇEKME");
    }

    /// <summary>
    /// Extract a numeric value from a line that falls within the expected range.
    /// Picks the last matching number (cross rates typically appear at end of row).
    /// </summary>
    private static double ExtractCrossRate(string line, double min, double max)
    {
        var matches = Regex.Matches(line, @"\d+[.,]\d+");
        double found = 0;
        foreach (Match m in matches)
        {
            var val = TurkishNumberHelper.ParseImported(m.Value);
            if (val > min && val < max)
                found = val; // keep last match (cross rate is typically rightmost)
        }
        return found;
    }

    /// <summary>
    /// Token-based fallback: find a label token, then search nearby tokens for a value in range.
    /// </summary>
    private static double FindRateByToken(string[] tokens, string[] labels, double min, double max, int searchWindow)
    {
        for (int i = 0; i < tokens.Length; i++)
        {
            var upper = tokens[i].ToUpperInvariant();
            if (!labels.Any(l => upper.Contains(l))) continue;

            for (int j = i + 1; j < Math.Min(i + searchWindow, tokens.Length); j++)
            {
                var val = TurkishNumberHelper.ParseImported(tokens[j]);
                if (val > min && val < max)
                    return val;
            }
        }
        return 0;
    }

    /// <summary>
    /// Extract page text from PDF using iText7.
    /// Enforces page count and total text length limits.
    /// </summary>
    private static List<string> ExtractPageTexts(string filePath)
    {
        var pages = new List<string>();

        using var pdfReader = new PdfReader(filePath);
        using var pdfDoc = new PdfDocument(pdfReader);

        var totalPages = pdfDoc.GetNumberOfPages();
        if (totalPages > MaxPageCount)
            throw new InvalidOperationException(
                $"PDF çok fazla sayfa içeriyor ({totalPages}). Maksimum {MaxPageCount} sayfa desteklenmektedir.");

        int totalTextLength = 0;
        for (int pageNum = 1; pageNum <= totalPages; pageNum++)
        {
            var page = pdfDoc.GetPage(pageNum);
            var strategy = new SimpleTextExtractionStrategy();
            var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);

            totalTextLength += pageText.Length;
            if (totalTextLength > MaxTextLength)
                throw new InvalidOperationException(
                    $"PDF metin içeriği çok büyük. Maksimum {MaxTextLength:N0} karakter desteklenmektedir.");

            pages.Add(pageText);
            Debug.WriteLine($"[PdfParser] Page {pageNum} text length: {pageText.Length}");
        }

        return pages;
    }
}
