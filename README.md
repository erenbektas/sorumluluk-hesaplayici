# Sorumluluk Hesaplayıcı

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-Desktop-0078D4?logo=windows&logoColor=white)
![Platform](https://img.shields.io/badge/Platform-Windows%20x64-0078D6?logo=windows&logoColor=white)
![iText7](https://img.shields.io/badge/iText7-8.0.5-E34F26?logo=adobeacrobatreader&logoColor=white)
![PDFsharp](https://img.shields.io/badge/PDFsharp-6.1.1-E34F26?logo=adobeacrobatreader&logoColor=white)

Uluslararası taşıma sözleşmelerine göre taşıyıcının azami sorumluluk tutarını (SDR hesabı) hesaplayan Windows masaüstü uygulaması.

## Ne Yapar?

TCMB (Türkiye Cumhuriyet Merkez Bankası) döviz kuru PDF'inden EUR/USD ve SDR/USD kurlarını otomatik olarak okur, hasarlı emtianın brüt ağırlığı üzerinden SDR hesabı yapar ve sonucu tespit tutarıyla karşılaştırır. Hesaplama sonucunu Word ve Outlook'a yapıştırılabilir biçimde panoya kopyalar.

## Kullanım

1. **PDF yükle** — TCMB döviz kuru belgesini sürükleyip bırakın veya dosya seçiciyi kullanın.
2. **Verileri girin** — yükleme tarihi (opsiyonel), hasarlı emtia brüt kg, tespit tutarı (EUR) ve taşıma türünü (karayolu/denizyolu/diğer) belirleyin.
3. **Hesapla** — SDR tutarı, USD ve EUR karşılıkları hesaplanır. SDR hesabı ile tespit tutarı karşılaştırılarak hangisinin dikkate alınacağı belirlenir.
4. **Kopyala** — sonuç metnini panoya kopyalayın (RTF, HTML ve düz metin formatlarında). Doğrudan Word veya Outlook'a yapıştırılabilir.

## Özellikler

- Koyu tema arayüz
- TCMB PDF'inden otomatik kur okuma (tarih, EUR/USD, SDR/USD)
- Karayolu (CMR — 8,33 SDR), denizyolu (2,00 SDR) ve özel SDR sabiti desteği
- SDR tutarı ile tespit tutarı karşılaştırması
- Tarih uyumsuzluğu uyarıları (yükleme tarihi ≠ kur tarihi)
- Türkçe sayı formatı desteği (1.234,56)
- Panoya kopyalama: RTF + HTML + düz metin (Word/Outlook uyumlu)
- Türkçe karakter desteği (ç, ğ, ı, ö, ş, ü) tüm çıktı formatlarında
- Sürükle-bırak ile PDF yükleme
- Bağımsız .exe olarak dağıtılabilir

## Gereksinimler

- Windows 10/11 (x64)
- .NET 8 runtime (self-contained derlemede dahildir)

## Derleme

```bash
dotnet build src/SorumlulukHesaplama/SorumlulukHesaplama.csproj
```

Tek bir self-contained çalıştırılabilir dosya olarak yayınlamak için:

```bash
dotnet publish src/SorumlulukHesaplama/SorumlulukHesaplama.csproj -c Release
```

## Testler

```bash
dotnet test
```

## Proje Yapısı

```
src/SorumlulukHesaplama/
├── Models/
│   ├── CalculationInput.cs       # Hesaplama giriş parametreleri
│   ├── CalculationResult.cs      # Hesaplama sonuç modeli
│   ├── ExchangeData.cs           # PDF'den okunan döviz kuru verileri
│   └── TransportType.cs          # Taşıma türü enum (Karayolu/Denizyolu/Diğer)
├── ViewModels/
│   ├── MainViewModel.cs          # Uygulama mantığı ve durum yönetimi
│   └── RelayCommand.cs           # ICommand implementasyonu
├── Services/
│   ├── ClipboardService.cs       # RTF/HTML/düz metin panoya kopyalama
│   ├── PdfExchangeRateParser.cs  # TCMB PDF'inden kur okuma
│   ├── SdrCalculator.cs          # SDR hesaplama motoru
│   └── TurkishNumberHelper.cs    # Türkçe sayı format dönüşümleri
├── MainWindow.xaml               # Arayüz tasarımı
├── MainWindow.xaml.cs            # Arayüz kod arkası
├── InfoWindow.xaml               # Hakkında penceresi
└── App.xaml                      # Uygulama kaynakları ve stiller

tests/SorumlulukHesaplama.Tests/
├── SdrCalculatorTests.cs         # Hesaplama motoru testleri
├── TurkishNumberHelperTests.cs   # Sayı format testleri
└── ClipboardServiceTests.cs      # Panoya kopyalama testleri
```

## Kullanılan Kütüphaneler

- [iText7](https://github.com/itext/itext-dotnet) — PDF metin çıkarma (TCMB kur belgesi okuma)
- [PDFsharp](https://github.com/empira/PDFsharp) — PDF işleme
