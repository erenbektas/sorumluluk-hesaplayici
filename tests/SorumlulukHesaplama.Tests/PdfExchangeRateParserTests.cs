using System.IO;
using SorumlulukHesaplama.Services;

namespace SorumlulukHesaplama.Tests;

public class PdfExchangeRateParserTests
{
    [Fact]
    public void Parse_NonExistentFile_Throws()
    {
        Assert.ThrowsAny<Exception>(() =>
            PdfExchangeRateParser.Parse("nonexistent.pdf"));
    }

    [Fact]
    public void Parse_OversizedFile_ThrowsWithMessage()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            using (var fs = File.OpenWrite(tempFile))
            {
                var buffer = new byte[1024 * 1024]; // 1 MB chunk
                for (int i = 0; i <= PdfExchangeRateParser.MaxFileSizeBytes / buffer.Length; i++)
                    fs.Write(buffer, 0, buffer.Length);
            }

            var ex = Assert.Throws<InvalidOperationException>(() =>
                PdfExchangeRateParser.Parse(tempFile));
            Assert.Contains("Dosya boyutu", ex.Message);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void GuardrailConstants_AreReasonable()
    {
        Assert.Equal(10 * 1024 * 1024, PdfExchangeRateParser.MaxFileSizeBytes);
        Assert.Equal(10, PdfExchangeRateParser.MaxPageCount);
        Assert.Equal(500_000, PdfExchangeRateParser.MaxTextLength);
    }
}
