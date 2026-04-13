using SorumlulukHesaplama.Services;

namespace SorumlulukHesaplama.Tests;

public class TurkishNumberHelperTests
{
    [Theory]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("1234,56", 1234.56)]
    [InlineData("1234", 1234)]
    [InlineData("0,5", 0.5)]
    [InlineData("1.000.000,99", 1000000.99)]
    public void Parse_ValidTurkishNumbers_ReturnsCorrectValue(string input, double expected)
    {
        Assert.Equal(expected, TurkishNumberHelper.Parse(input), 2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData(null)]
    public void TryParse_InvalidInput_ReturnsFalse(string? input)
    {
        Assert.False(TurkishNumberHelper.TryParse(input!, out _));
    }

    [Theory]
    [InlineData("1.234,56", true, 1234.56)]
    [InlineData("100", true, 100)]
    [InlineData("abc", false, 0)]
    public void TryParse_ReturnsExpected(string input, bool expectedSuccess, double expectedValue)
    {
        var success = TurkishNumberHelper.TryParse(input, out var value);
        Assert.Equal(expectedSuccess, success);
        if (expectedSuccess)
            Assert.Equal(expectedValue, value, 2);
    }

    [Fact]
    public void Parse_ReturnsZeroOnInvalid()
    {
        Assert.Equal(0, TurkishNumberHelper.Parse("abc"));
        Assert.Equal(0, TurkishNumberHelper.Parse(""));
    }

    [Theory]
    [InlineData(1234.56, 2, "1.234,56")]
    [InlineData(0.5, 2, "0,50")]
    public void Format_TurkishLocale(double value, int decimals, string expected)
    {
        Assert.Equal(expected, TurkishNumberHelper.Format(value, decimals));
    }

    [Theory]
    [InlineData("31122025", "31.12.2025")]
    [InlineData("01.02.2025", "01.02.2025")]
    public void FormatDateInput_AutoFormats(string input, string expected)
    {
        Assert.Equal(expected, TurkishNumberHelper.FormatDateInput(input));
    }

    // --- Imported number parsing (flexible for PDF tokens) ---

    [Theory]
    [InlineData("1.2345", 1.2345)]       // dot-decimal (TCMB style)
    [InlineData("1,2345", 1.2345)]       // comma-decimal
    [InlineData("1.08", 1.08)]           // dot-decimal EUR rate
    [InlineData("1.000,99", 1000.99)]    // Turkish grouped with comma decimal
    [InlineData("1,000.99", 1000.99)]    // English grouped with dot decimal
    [InlineData("1000", 1000)]           // plain integer
    [InlineData("0,5", 0.5)]            // comma decimal, no integer leading digits
    [InlineData("0.5", 0.5)]            // dot decimal
    [InlineData("1.000", 1000)]          // Turkish thousands (exactly 3 digits after dot)
    [InlineData("12.345.678", 12345678)] // Turkish grouped thousands
    public void ParseImported_FlexibleFormats(string input, double expected)
    {
        Assert.Equal(expected, TurkishNumberHelper.ParseImported(input), 4);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData(null)]
    public void TryParseImportedNumber_InvalidInput_ReturnsFalse(string? input)
    {
        Assert.False(TurkishNumberHelper.TryParseImportedNumber(input!, out _));
    }

    [Fact]
    public void ParseImported_ReturnsZeroOnInvalid()
    {
        Assert.Equal(0, TurkishNumberHelper.ParseImported("abc"));
        Assert.Equal(0, TurkishNumberHelper.ParseImported(""));
    }

    [Fact]
    public void TryParse_DotOnly_TreatsAsTurkishThousands()
    {
        // Existing TryParse: dot-only is thousand separator (Turkish convention)
        Assert.True(TurkishNumberHelper.TryParse("1.234", out var val));
        Assert.Equal(1234, val, 0);
    }

    [Fact]
    public void TryParseImportedNumber_DotOnly_TreatsAsDecimalWhenNotGroupedPattern()
    {
        // Imported parser: 1.234 is NOT a grouped pattern (would need 1.234 to match ^\d{1,3}(\.\d{3})+$)
        // Wait — 1.234 does match ^\d{1,3}(\.\d{3})+$  (1 followed by .234 which is 3 digits)
        // So 1.234 is ambiguous. The heuristic treats it as grouped thousands.
        Assert.True(TurkishNumberHelper.TryParseImportedNumber("1.234", out var val));
        Assert.Equal(1234, val, 0);

        // But 1.2345 does NOT match the grouped pattern → treated as decimal
        Assert.True(TurkishNumberHelper.TryParseImportedNumber("1.2345", out var val2));
        Assert.Equal(1.2345, val2, 4);

        // And 1.08 does NOT match the grouped pattern → treated as decimal
        Assert.True(TurkishNumberHelper.TryParseImportedNumber("1.08", out var val3));
        Assert.Equal(1.08, val3, 4);
    }
}
