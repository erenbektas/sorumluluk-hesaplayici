using SorumlulukHesaplama.Models;
using SorumlulukHesaplama.Services;

namespace SorumlulukHesaplama.Tests;

public class SdrCalculatorTests
{
    [Theory]
    [InlineData(TransportType.Road, 8.33)]
    [InlineData(TransportType.Sea, 2.00)]
    public void GetSdrConstant_ReturnsCorrectValue(TransportType type, double expected)
    {
        Assert.Equal(expected, SdrCalculator.GetSdrConstant(type));
    }

    [Fact]
    public void GetSdrConstant_Other_UsesCustomValue()
    {
        Assert.Equal(5.0, SdrCalculator.GetSdrConstant(TransportType.Other, 5.0));
    }

    [Fact]
    public void GetSdrConstant_Other_DefaultsTo1()
    {
        Assert.Equal(1.0, SdrCalculator.GetSdrConstant(TransportType.Other));
    }

    [Fact]
    public void Calculate_SdrLowerThanAssessment_UsesSdrLimit()
    {
        var input = new CalculationInput
        {
            GrossKg = 100,
            AssessmentAmountEur = 10000,
            TransportType = TransportType.Road,
            ExchangeData = new ExchangeData
            {
                Date = "15.01.2025",
                EurUsdRate = 1.08,
                SdrUsdRate = 1.33
            }
        };

        var result = SdrCalculator.Calculate(input);

        // 100 * 8.33 = 833 SDR, 833 * 1.33 = 1107.89 USD, / 1.08 = ~1025.82 EUR
        Assert.True(result.UseSdrLimit);
        Assert.Equal(833, result.SdrAmount, 0);
        Assert.True(result.SdrAmountEur < input.AssessmentAmountEur);
    }

    [Fact]
    public void Calculate_SdrHigherThanAssessment_UsesAssessment()
    {
        var input = new CalculationInput
        {
            GrossKg = 10000,
            AssessmentAmountEur = 500,
            TransportType = TransportType.Road,
            ExchangeData = new ExchangeData
            {
                Date = "15.01.2025",
                EurUsdRate = 1.08,
                SdrUsdRate = 1.33
            }
        };

        var result = SdrCalculator.Calculate(input);
        Assert.False(result.UseSdrLimit);
    }

    [Fact]
    public void Calculate_WithLoadingDateAfterTableDate_WarningFuture()
    {
        var input = new CalculationInput
        {
            LoadingDate = "20.01.2025",
            GrossKg = 100,
            AssessmentAmountEur = 10000,
            TransportType = TransportType.Road,
            ExchangeData = new ExchangeData
            {
                Date = "15.01.2025",
                EurUsdRate = 1.08,
                SdrUsdRate = 1.33
            }
        };

        var result = SdrCalculator.Calculate(input);
        Assert.Equal(DateWarning.Future, result.DateWarning);
    }

    [Fact]
    public void Calculate_WithLoadingDateBeforeTableDate_WarningPast()
    {
        var input = new CalculationInput
        {
            LoadingDate = "10.01.2025",
            GrossKg = 100,
            AssessmentAmountEur = 10000,
            TransportType = TransportType.Road,
            ExchangeData = new ExchangeData
            {
                Date = "15.01.2025",
                EurUsdRate = 1.08,
                SdrUsdRate = 1.33
            }
        };

        var result = SdrCalculator.Calculate(input);
        Assert.Equal(DateWarning.Past, result.DateWarning);
    }

    [Fact]
    public void Calculate_NoLoadingDate_UsesTableDate()
    {
        var input = new CalculationInput
        {
            GrossKg = 100,
            AssessmentAmountEur = 10000,
            TransportType = TransportType.Sea,
            ExchangeData = new ExchangeData
            {
                Date = "15.01.2025",
                EurUsdRate = 1.08,
                SdrUsdRate = 1.33
            }
        };

        var result = SdrCalculator.Calculate(input);
        Assert.Equal("15.01.2025", result.EffectiveLoadingDate);
        Assert.Null(result.DateWarning);
    }

    [Fact]
    public void Calculate_SeaTransport_Uses2SdrConstant()
    {
        var input = new CalculationInput
        {
            GrossKg = 500,
            AssessmentAmountEur = 50000,
            TransportType = TransportType.Sea,
            ExchangeData = new ExchangeData
            {
                Date = "15.01.2025",
                EurUsdRate = 1.08,
                SdrUsdRate = 1.33
            }
        };

        var result = SdrCalculator.Calculate(input);
        Assert.Equal(2.0, result.SdrConstant);
        Assert.Equal(1000, result.SdrAmount, 0); // 500 * 2
    }

    [Fact]
    public void Calculate_ResultTextContainsTurkishCharacters()
    {
        var input = new CalculationInput
        {
            GrossKg = 100,
            AssessmentAmountEur = 10000,
            TransportType = TransportType.Road,
            ExchangeData = new ExchangeData
            {
                Date = "15.01.2025",
                EurUsdRate = 1.08,
                SdrUsdRate = 1.33
            }
        };

        var result = SdrCalculator.Calculate(input);
        Assert.Contains("hesaplanmıştır", result.ResultText);
        Assert.Contains("SDR", result.ResultText);
        Assert.Contains("<b>", result.ResultHtml);
    }
}
