using System.Globalization;
using SorumlulukHesaplama.Models;

namespace SorumlulukHesaplama.Services;

public static class SdrCalculator
{
    public static double GetSdrConstant(TransportType type, double? customValue = null)
    {
        return type switch
        {
            TransportType.Road => 8.33,
            TransportType.Sea => 2.00,
            TransportType.Other => customValue ?? 1.00,
            _ => 1.00
        };
    }

    public static string GetTransportDescription(TransportType type)
    {
        return type switch
        {
            TransportType.Road => "CMR hükümlerine göre",
            TransportType.Sea => "Denizyolu taşıma hükümlerine göre",
            TransportType.Other => "Taşıma hükümlerine göre",
            _ => "Taşıma hükümlerine göre"
        };
    }

    private static DateTime ParseDate(string dateStr)
    {
        if (DateTime.TryParseExact(dateStr, "dd.MM.yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;
        throw new FormatException($"Geçersiz tarih formatı: {dateStr}");
    }

    public static CalculationResult Calculate(CalculationInput input)
    {
        var effectiveLoadingDate = input.LoadingDate ?? input.ExchangeData.Date;
        var sdrConstant = GetSdrConstant(input.TransportType, input.CustomSdrConstant);

        var sdrAmount = input.GrossKg * sdrConstant;
        var sdrAmountUsd = sdrAmount * input.ExchangeData.SdrUsdRate;
        var sdrAmountEur = sdrAmountUsd / input.ExchangeData.EurUsdRate;
        var useSdrLimit = sdrAmountEur < input.AssessmentAmountEur;

        // Date warning
        DateWarning? dateWarning = null;
        if (!string.IsNullOrEmpty(input.LoadingDate))
        {
            var loadDate = ParseDate(input.LoadingDate);
            var tableDate = ParseDate(input.ExchangeData.Date);
            if (loadDate < tableDate) dateWarning = DateWarning.Past;
            else if (loadDate > tableDate) dateWarning = DateWarning.Future;
        }

        // Format values
        var f = TurkishNumberHelper.Format;
        var fr = TurkishNumberHelper.FormatRate;

        var grossKgF = f(input.GrossKg);
        var sdrConstantF = f(sdrConstant);
        var sdrAmountF = f(sdrAmount);
        var sdrUsdRateF = fr(input.ExchangeData.SdrUsdRate);
        var sdrAmountUsdF = f(sdrAmountUsd);
        var eurUsdRateF = fr(input.ExchangeData.EurUsdRate);
        var sdrAmountEurF = f(sdrAmountEur);
        var assessmentEurF = f(input.AssessmentAmountEur);

        var transportDesc = GetTransportDescription(input.TransportType);
        var cmrText = input.TransportType == TransportType.Road ? "CMR'dan" : "taşıma sözleşmesinden";

        // Plain text
        var resultText = $"{transportDesc}, hasarlı emtianın brüt ağırlığı üzerinden SDR hesabı yapılmaktadır. " +
            $"Bu hadise neticesinde hasarlı emtia ({grossKgF} Brüt Kg) üzerinden taşıyıcının {cmrText} doğan azami sorumluluğu aşağıdaki gibi hesaplanmıştır.\n\n" +
            $"Toplam SDR:\t\t{grossKgF} Kg x {sdrConstantF} SDR = {sdrAmountF} SDR\n" +
            $"Toplam USD:\t\t{sdrAmountF} SDR x {sdrUsdRateF} SDR/USD = {sdrAmountUsdF} USD\n" +
            $"Toplam EUR:\t\t{sdrAmountUsdF} USD \u00F7 {eurUsdRateF} EUR/USD = {sdrAmountEurF} EUR\n\n";

        if (useSdrLimit)
            resultText += $"\u2192 Yapılan SDR hesabı ({sdrAmountEurF} EUR), hasarlı emtia tutarı fatura bedeli üzerinden yapılan hesaplamaya ({assessmentEurF} EUR) göre düşük olduğundan, hesaplamada SDR tespit tutarı dikkate alınmıştır.\n\n";
        else
            resultText += $"\u2192 Yapılan SDR hesabı ({sdrAmountEurF} EUR), hasarlı emtia tutarı fatura bedeli üzerinden yapılan hesaplamaya ({assessmentEurF} EUR) göre yüksek olduğundan, hesaplamada tespit tutarı dikkate alınmıştır.\n\n";

        if (dateWarning == DateWarning.Past)
            resultText += $"\u2192 Not: Hesaplamada ürünlerin yüklemesinin yapıldığı tarih resmî tatile denk geldiği için bir önceki iş günü olan {input.ExchangeData.Date} tarihindeki TCMB döviz kuru verileri dikkate alınmıştır.\n";
        else
            resultText += $"\u2192 Not: Hesaplamada ürünlerin yüklemesinin yapıldığı {effectiveLoadingDate} tarihindeki TCMB döviz kuru verileri dikkate alınmıştır.\n";

        resultText += $"1 SDR = {sdrUsdRateF} USD    |    1 EUR = {eurUsdRateF} USD";

        // HTML
        var resultHtml = $"<p style=\"text-align:justify;\">{transportDesc}, hasarlı emtianın brüt ağırlığı üzerinden SDR hesabı yapılmaktadır. " +
            $"Bu hadise neticesinde hasarlı emtia <b>({grossKgF} Brüt Kg)</b> üzerinden taşıyıcının {cmrText} doğan azami sorumluluğu aşağıdaki gibi hesaplanmıştır.</p>";

        resultHtml += "<p style=\"text-align:left;\">" +
            $"Toplam SDR:<span style=\"white-space:pre;\">&#9;&#9;</span><i>{grossKgF} Kg x {sdrConstantF} SDR =</i> <b><i>{sdrAmountF} SDR</i></b><br/>" +
            $"Toplam USD:<span style=\"white-space:pre;\">&#9;&#9;</span><i>{sdrAmountF} SDR x {sdrUsdRateF} SDR/USD =</i> <b><i>{sdrAmountUsdF} USD</i></b><br/>" +
            $"<b><i>Toplam EUR:</i></b><span style=\"white-space:pre;\">&#9;&#9;</span><i>{sdrAmountUsdF} USD \u00F7 {eurUsdRateF} EUR/USD =</i> <b><i>{sdrAmountEurF} EUR</i></b>*</p>";

        if (useSdrLimit)
            resultHtml += $"<p style=\"text-align:justify;\"><b>\u2192</b>&#9;Yapılan SDR hesabı <b><i><span style=\"color:red;\">({sdrAmountEurF} EUR)</span></i></b>, hasarlı emtia tutarı fatura bedeli üzerinden yapılan hesaplamaya <b><i>({assessmentEurF} EUR)</i></b> göre düşük olduğundan, hesaplamada <b>SDR tespit tutarı</b> dikkate alınmıştır.</p>";
        else
            resultHtml += $"<p style=\"text-align:justify;\"><b>\u2192</b>&#9;Yapılan SDR hesabı <b><i>({sdrAmountEurF} EUR)</i></b>, hasarlı emtia tutarı fatura bedeli üzerinden yapılan hesaplamaya <b><i>({assessmentEurF} EUR)</i></b> göre yüksek olduğundan, hesaplamada tespit tutarı dikkate alınmıştır.</p>";

        if (dateWarning == DateWarning.Past)
            resultHtml += $"<p style=\"text-align:justify;\"><b>\u2192</b>&#9;<b>Not:</b> Hesaplamada ürünlerin yüklemesinin yapıldığı tarih resmî tatile denk geldiği için bir önceki iş günü olan {input.ExchangeData.Date} tarihindeki TCMB döviz kuru verileri dikkate alınmıştır.</p>";
        else
            resultHtml += $"<p style=\"text-align:justify;\"><b>\u2192</b>&#9;<b>Not:</b> Hesaplamada ürünlerin yüklemesinin yapıldığı {effectiveLoadingDate} tarihindeki TCMB döviz kuru verileri dikkate alınmıştır.</p>";

        resultHtml += $"<p style=\"text-align:center;\">1 SDR = {sdrUsdRateF} USD &#160;&#160;|&#160;&#160; 1 EUR = {eurUsdRateF} USD</p>";

        return new CalculationResult
        {
            EffectiveLoadingDate = effectiveLoadingDate,
            SdrConstant = sdrConstant,
            GrossKg = input.GrossKg,
            SdrAmount = sdrAmount,
            SdrAmountUsd = sdrAmountUsd,
            SdrAmountEur = sdrAmountEur,
            AssessmentAmountEur = input.AssessmentAmountEur,
            SdrUsdRate = input.ExchangeData.SdrUsdRate,
            EurUsdRate = input.ExchangeData.EurUsdRate,
            UseSdrLimit = useSdrLimit,
            DateWarning = dateWarning,
            ResultText = resultText,
            ResultHtml = resultHtml
        };
    }
}
