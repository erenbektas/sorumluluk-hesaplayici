namespace SorumlulukHesaplama.Models;

public class ExchangeData
{
    public string Date { get; set; } = "";       // dd.mm.yyyy
    public double EurUsdRate { get; set; }        // EUR/USD cross rate
    public double SdrUsdRate { get; set; }        // SDR/USD rate
}
