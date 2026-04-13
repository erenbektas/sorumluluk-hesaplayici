namespace SorumlulukHesaplama.Models;

public class CalculationResult
{
    public string EffectiveLoadingDate { get; set; } = "";
    public double SdrConstant { get; set; }
    public double GrossKg { get; set; }

    public double SdrAmount { get; set; }
    public double SdrAmountUsd { get; set; }
    public double SdrAmountEur { get; set; }
    public double AssessmentAmountEur { get; set; }

    public double SdrUsdRate { get; set; }
    public double EurUsdRate { get; set; }

    public bool UseSdrLimit { get; set; }
    public DateWarning? DateWarning { get; set; }

    public string ResultText { get; set; } = "";
    public string ResultHtml { get; set; } = "";
}

public enum DateWarning
{
    Past,
    Future
}
