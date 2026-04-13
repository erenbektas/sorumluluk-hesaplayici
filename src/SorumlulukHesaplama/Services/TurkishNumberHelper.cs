using System.Globalization;
using System.Text.RegularExpressions;

namespace SorumlulukHesaplama.Services;

public static class TurkishNumberHelper
{
    private static readonly CultureInfo TrCulture = new("tr-TR");

    /// <summary>
    /// Parse Turkish number format (1.234,56 or 1,2345) to double.
    /// Preserves full precision.
    /// </summary>
    public static bool TryParse(string str, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(str)) return false;

        var normalized = str.Trim();
        if (!Regex.IsMatch(normalized, @"\d")) return false;

        // If it contains both . and , then . is thousand separator, , is decimal
        if (normalized.Contains('.') && normalized.Contains(','))
        {
            normalized = normalized.Replace(".", "").Replace(",", ".");
        }
        // If it only contains , then , is decimal separator
        else if (normalized.Contains(','))
        {
            normalized = normalized.Replace(",", ".");
        }

        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// Parse Turkish number format. Returns 0 on failure.
    /// Only use after prior validation; prefer TryParse for user input.
    /// </summary>
    public static double Parse(string str)
    {
        return TryParse(str, out var result) ? result : 0;
    }

    /// <summary>
    /// Format number in Turkish locale (1.234,56)
    /// </summary>
    public static string Format(double num, int decimals = 2)
    {
        return num.ToString($"N{decimals}", TrCulture);
    }

    /// <summary>
    /// Format rate with appropriate decimals (preserve precision, min 4, max 6)
    /// </summary>
    public static string FormatRate(double num)
    {
        var str = num.ToString(CultureInfo.InvariantCulture);
        var decimalPlaces = str.Contains('.') ? str.Split('.')[1].Length : 0;
        var decimals = Math.Max(decimalPlaces, 4);
        decimals = Math.Min(decimals, 6);
        return num.ToString($"N{decimals}", TrCulture);
    }

    /// <summary>
    /// Format input as Turkish number while typing (add thousand separators).
    /// </summary>
    public static string FormatInput(string value)
    {
        // Remove all non-digit and non-comma characters
        var cleaned = Regex.Replace(value, @"[^\d,]", "");

        // Handle multiple commas - keep only the first
        var parts = cleaned.Split(',');
        if (parts.Length > 2)
        {
            cleaned = parts[0] + "," + string.Join("", parts.Skip(1));
        }
        else if (parts.Length == 2)
        {
            cleaned = parts[0] + "," + parts[1];
        }

        // Add thousand separators to integer part
        if (parts[0].Length > 0)
        {
            var intPart = parts[0];
            // Add dots as thousand separators
            var formatted = "";
            for (int i = 0; i < intPart.Length; i++)
            {
                if (i > 0 && (intPart.Length - i) % 3 == 0)
                    formatted += ".";
                formatted += intPart[i];
            }
            cleaned = formatted + (parts.Length > 1 ? "," + parts[1] : "");
        }

        return cleaned;
    }

    /// <summary>
    /// Auto-format date input as dd.mm.yyyy
    /// </summary>
    public static string FormatDateInput(string value)
    {
        var cleaned = Regex.Replace(value, @"[^\d.]", "");

        // Auto-add dots
        if (cleaned.Length >= 2 && !cleaned.Contains('.'))
        {
            cleaned = cleaned[..2] + "." + cleaned[2..];
        }
        if (cleaned.Length >= 5 && cleaned.Split('.').Length < 3)
        {
            cleaned = cleaned[..5] + "." + cleaned[5..];
        }

        // Limit length
        if (cleaned.Length > 10) cleaned = cleaned[..10];

        return cleaned;
    }
}
