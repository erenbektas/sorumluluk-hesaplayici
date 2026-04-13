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
}
