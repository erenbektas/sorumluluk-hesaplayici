using SorumlulukHesaplama.Services;
using System.Reflection;

namespace SorumlulukHesaplama.Tests;

public class ClipboardServiceTests
{
    private static string InvokeEncodeTurkish(string input)
    {
        var method = typeof(ClipboardService).GetMethod("EncodeTurkishForRtf",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string)method.Invoke(null, [input])!;
    }

    [Theory]
    [InlineData("ç", "\\'e7")]
    [InlineData("Ç", "\\'c7")]
    [InlineData("ğ", "\\'f0")]
    [InlineData("Ğ", "\\'d0")]
    [InlineData("ı", "\\'fd")]
    [InlineData("İ", "\\'dd")]
    [InlineData("ö", "\\'f6")]
    [InlineData("Ö", "\\'d6")]
    [InlineData("ş", "\\'fe")]
    [InlineData("Ş", "\\'de")]
    [InlineData("ü", "\\'fc")]
    [InlineData("Ü", "\\'dc")]
    public void EncodeTurkishForRtf_EncodesAllTurkishCharacters(string input, string expected)
    {
        Assert.Equal(expected, InvokeEncodeTurkish(input));
    }

    [Fact]
    public void EncodeTurkishForRtf_EncodesArrow()
    {
        Assert.Equal("\\u8594?", InvokeEncodeTurkish("\u2192"));
    }

    [Fact]
    public void EncodeTurkishForRtf_PreservesAscii()
    {
        Assert.Equal("Hello World", InvokeEncodeTurkish("Hello World"));
    }

    [Fact]
    public void EncodeTurkishForRtf_MixedContent()
    {
        var result = InvokeEncodeTurkish("Brüt ağırlığı üzerinden");
        Assert.Contains("\\'fc", result);  // ü
        Assert.Contains("\\'f0", result);  // ğ
        Assert.Contains("\\'fd", result);  // ı
        Assert.DoesNotContain("ü", result);
        Assert.DoesNotContain("ğ", result);
        Assert.DoesNotContain("ı", result);
    }
}
