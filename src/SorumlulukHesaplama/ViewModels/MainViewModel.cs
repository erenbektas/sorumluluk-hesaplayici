using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using SorumlulukHesaplama.Models;
using SorumlulukHesaplama.Services;

namespace SorumlulukHesaplama.ViewModels;

public enum AppPage { Upload, Form, Result }

public class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // --- State ---
    private AppPage _currentPage = AppPage.Upload;
    public AppPage CurrentPage
    {
        get => _currentPage;
        set { _currentPage = value; OnPropertyChanged(nameof(CurrentPage)); }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); CommandManager.InvalidateRequerySuggested(); }
    }

    private string _statusText = "";
    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(nameof(StatusText)); }
    }

    private string _errorText = "";
    public string ErrorText
    {
        get => _errorText;
        set { _errorText = value; OnPropertyChanged(nameof(ErrorText)); }
    }

    private bool _copied;
    public bool Copied
    {
        get => _copied;
        set { _copied = value; OnPropertyChanged(nameof(Copied)); }
    }

    // --- Exchange Data ---
    private ExchangeData? _exchangeData;
    public ExchangeData? ExchangeData
    {
        get => _exchangeData;
        set { _exchangeData = value; OnPropertyChanged(nameof(ExchangeData)); }
    }

    // --- Form Fields ---
    private string _loadingDate = "";
    public string LoadingDate
    {
        get => _loadingDate;
        set { _loadingDate = TurkishNumberHelper.FormatDateInput(value); OnPropertyChanged(nameof(LoadingDate)); }
    }

    private string _grossKgText = "";
    public string GrossKgText
    {
        get => _grossKgText;
        set { _grossKgText = TurkishNumberHelper.FormatInput(value); OnPropertyChanged(nameof(GrossKgText)); }
    }

    private string _assessmentEurText = "";
    public string AssessmentEurText
    {
        get => _assessmentEurText;
        set { _assessmentEurText = TurkishNumberHelper.FormatInput(value); OnPropertyChanged(nameof(AssessmentEurText)); }
    }

    private TransportType _transportType = TransportType.Road;
    public TransportType TransportType
    {
        get => _transportType;
        set
        {
            _transportType = value;
            OnPropertyChanged(nameof(TransportType));
            OnPropertyChanged(nameof(IsRoad));
            OnPropertyChanged(nameof(IsSea));
            OnPropertyChanged(nameof(IsOther));
            OnPropertyChanged(nameof(SdrConstantDisplay));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool IsRoad
    {
        get => _transportType == TransportType.Road;
        set { if (value) TransportType = TransportType.Road; }
    }
    public bool IsSea
    {
        get => _transportType == TransportType.Sea;
        set { if (value) TransportType = TransportType.Sea; }
    }
    public bool IsOther
    {
        get => _transportType == TransportType.Other;
        set { if (value) TransportType = TransportType.Other; }
    }

    private string _customSdrText = "";
    public string CustomSdrText
    {
        get => _customSdrText;
        set { _customSdrText = TurkishNumberHelper.FormatInput(value); OnPropertyChanged(nameof(CustomSdrText)); OnPropertyChanged(nameof(SdrConstantDisplay)); }
    }

    public string SdrConstantDisplay
    {
        get
        {
            if (_transportType == TransportType.Other)
            {
                var val = TurkishNumberHelper.Parse(_customSdrText);
                return val > 0 ? TurkishNumberHelper.Format(val) : "?";
            }
            return TurkishNumberHelper.Format(SdrCalculator.GetSdrConstant(_transportType));
        }
    }

    // --- Validation ---
    private string _dateError = "";
    public string DateError { get => _dateError; set { _dateError = value; OnPropertyChanged(nameof(DateError)); } }
    private string _kgError = "";
    public string KgError { get => _kgError; set { _kgError = value; OnPropertyChanged(nameof(KgError)); } }
    private string _eurError = "";
    public string EurError { get => _eurError; set { _eurError = value; OnPropertyChanged(nameof(EurError)); } }
    private string _sdrError = "";
    public string SdrError { get => _sdrError; set { _sdrError = value; OnPropertyChanged(nameof(SdrError)); } }

    // --- Result ---
    private CalculationResult? _result;
    public CalculationResult? Result
    {
        get => _result;
        set { _result = value; OnPropertyChanged(nameof(Result)); }
    }

    // --- Commands ---
    public ICommand BrowsePdfCommand { get; }
    public ICommand CalculateCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand ResetCommand { get; }

    public MainViewModel()
    {
        BrowsePdfCommand = new RelayCommand(_ => BrowsePdf(), _ => !IsLoading);
        CalculateCommand = new RelayCommand(_ => DoCalculate(), _ => CurrentPage == AppPage.Form && !IsLoading);
        CopyCommand = new RelayCommand(_ => DoCopy(), _ => Result != null);
        BackCommand = new RelayCommand(_ => GoBack(), _ => CurrentPage != AppPage.Upload);
        ResetCommand = new RelayCommand(_ => DoReset());
    }

    // --- PDF Loading ---
    private void BrowsePdf()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "PDF Dosyaları (*.pdf)|*.pdf",
            Title = "TCMB Döviz Kuru Belgesi Seçin"
        };

        if (dialog.ShowDialog() == true)
        {
            _ = LoadPdfAsync(dialog.FileName);
        }
    }

    public async Task LoadPdfAsync(string filePath)
    {
        IsLoading = true;
        ErrorText = "";
        StatusText = "PDF okunuyor...";

        try
        {
            var data = await Task.Run(() => PdfExchangeRateParser.Parse(filePath));
            ExchangeData = data;
            StatusText = "";
            CurrentPage = AppPage.Form;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] PDF parse error: {ex.Message}");
            ErrorText = ex.Message;
            StatusText = "";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void HandleFileDrop(string[] files)
    {
        if (CurrentPage != AppPage.Upload || IsLoading) return;

        var pdfFile = files.FirstOrDefault(f =>
            Path.GetExtension(f).Equals(".pdf", StringComparison.OrdinalIgnoreCase));

        if (pdfFile != null)
            _ = LoadPdfAsync(pdfFile);
        else
            ErrorText = "Lütfen bir PDF dosyası yükleyin.";
    }

    // --- Calculation ---
    private bool ValidateForm()
    {
        bool valid = true;
        DateError = KgError = EurError = SdrError = "";

        // Date validation: use DateTime.TryParseExact instead of regex
        if (!string.IsNullOrEmpty(_loadingDate))
        {
            if (!DateTime.TryParseExact(_loadingDate, "dd.MM.yyyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                DateError = "Geçerli bir tarih girin (gg.aa.yyyy)";
                valid = false;
            }
        }

        // Gross Kg: must parse and be > 0
        if (string.IsNullOrWhiteSpace(_grossKgText))
        {
            KgError = "Bu alan zorunludur";
            valid = false;
        }
        else if (!TurkishNumberHelper.TryParse(_grossKgText, out var grossKg) || grossKg <= 0)
        {
            KgError = "Geçerli bir sayı girin (sıfırdan büyük)";
            valid = false;
        }

        // Assessment EUR: must parse and be > 0
        if (string.IsNullOrWhiteSpace(_assessmentEurText))
        {
            EurError = "Bu alan zorunludur";
            valid = false;
        }
        else if (!TurkishNumberHelper.TryParse(_assessmentEurText, out var assessmentEur) || assessmentEur <= 0)
        {
            EurError = "Geçerli bir sayı girin (sıfırdan büyük)";
            valid = false;
        }

        // Custom SDR: must parse and be > 0 when "Other" transport type
        if (_transportType == TransportType.Other)
        {
            if (string.IsNullOrWhiteSpace(_customSdrText))
            {
                SdrError = "SDR sabiti zorunludur";
                valid = false;
            }
            else if (!TurkishNumberHelper.TryParse(_customSdrText, out var customSdr) || customSdr <= 0)
            {
                SdrError = "Geçerli bir sayı girin (sıfırdan büyük)";
                valid = false;
            }
        }

        return valid;
    }

    private void DoCalculate()
    {
        if (!ValidateForm() || ExchangeData == null) return;

        var input = new CalculationInput
        {
            LoadingDate = string.IsNullOrEmpty(_loadingDate) ? null : _loadingDate,
            GrossKg = TurkishNumberHelper.Parse(_grossKgText),
            AssessmentAmountEur = TurkishNumberHelper.Parse(_assessmentEurText),
            TransportType = _transportType,
            CustomSdrConstant = _transportType == TransportType.Other
                ? TurkishNumberHelper.Parse(_customSdrText) : null,
            ExchangeData = ExchangeData
        };

        Result = SdrCalculator.Calculate(input);
        CurrentPage = AppPage.Result;
    }

    // --- Copy ---
    private void DoCopy()
    {
        if (Result == null) return;

        try
        {
            ClipboardService.CopyToClipboard(Result.ResultHtml, Result.ResultText);
            Copied = true;
            _ = ResetCopiedAfterDelay();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] Copy error: {ex.Message}");
            MessageBox.Show("Kopyalama başarısız oldu.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ResetCopiedAfterDelay()
    {
        await Task.Delay(2000);
        Copied = false;
    }

    // --- Navigation ---
    private void GoBack()
    {
        if (CurrentPage == AppPage.Result)
        {
            CurrentPage = AppPage.Form;
            Result = null;
        }
        else if (CurrentPage == AppPage.Form)
        {
            CurrentPage = AppPage.Upload;
            ExchangeData = null;
        }
    }

    private void DoReset()
    {
        CurrentPage = AppPage.Upload;
        ExchangeData = null;
        Result = null;
        ErrorText = "";
        StatusText = "";
        LoadingDate = "";
        GrossKgText = "";
        AssessmentEurText = "";
        TransportType = TransportType.Road;
        CustomSdrText = "";
        DateError = KgError = EurError = SdrError = "";
    }
}
