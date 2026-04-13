namespace SorumlulukHesaplama.Models;

public class CalculationInput
{
    public string? LoadingDate { get; set; }          // dd.mm.yyyy or null
    public double GrossKg { get; set; }
    public double AssessmentAmountEur { get; set; }
    public TransportType TransportType { get; set; }
    public double? CustomSdrConstant { get; set; }    // required when Other
    public ExchangeData ExchangeData { get; set; } = new();
}
