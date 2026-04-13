using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using SorumlulukHesaplama.Models;
using SorumlulukHesaplama.Services;
using SorumlulukHesaplama.ViewModels;

namespace SorumlulukHesaplama;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Dark title bar
        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int darkMode = 1;
            DwmSetWindowAttribute(hwnd, 20, ref darkMode, sizeof(int));
            DwmSetWindowAttribute(hwnd, 19, ref darkMode, sizeof(int));
        };
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        base.OnClosed(e);
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.CurrentPage):
                UpdatePageVisibility();
                break;
            case nameof(MainViewModel.IsLoading):
                TxtLoading.Visibility = _viewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
                break;
            case nameof(MainViewModel.ErrorText):
                var hasError = !string.IsNullOrEmpty(_viewModel.ErrorText);
                ErrorBorder.Visibility = hasError ? Visibility.Visible : Visibility.Collapsed;
                TxtError.Text = _viewModel.ErrorText;
                break;
            case nameof(MainViewModel.ExchangeData):
                UpdateExchangeDisplay();
                break;
            case nameof(MainViewModel.Result):
                UpdateResultDisplay();
                break;
            case nameof(MainViewModel.Copied):
                UpdateCopyButton();
                break;
            case nameof(MainViewModel.TransportType):
                UpdateTransportDisplay();
                break;
            case nameof(MainViewModel.DateError):
                TxtDateError.Text = _viewModel.DateError;
                TxtDateError.Visibility = string.IsNullOrEmpty(_viewModel.DateError) ? Visibility.Collapsed : Visibility.Visible;
                break;
            case nameof(MainViewModel.KgError):
                TxtKgError.Text = _viewModel.KgError;
                TxtKgError.Visibility = string.IsNullOrEmpty(_viewModel.KgError) ? Visibility.Collapsed : Visibility.Visible;
                break;
            case nameof(MainViewModel.EurError):
                TxtEurError.Text = _viewModel.EurError;
                TxtEurError.Visibility = string.IsNullOrEmpty(_viewModel.EurError) ? Visibility.Collapsed : Visibility.Visible;
                break;
            case nameof(MainViewModel.SdrError):
                TxtSdrError.Text = _viewModel.SdrError;
                TxtSdrError.Visibility = string.IsNullOrEmpty(_viewModel.SdrError) ? Visibility.Collapsed : Visibility.Visible;
                break;
        }
    }

    private void UpdatePageVisibility()
    {
        var page = _viewModel.CurrentPage;

        UploadPage.Visibility = page == AppPage.Upload ? Visibility.Visible : Visibility.Collapsed;
        FormPage.Visibility = page == AppPage.Form ? Visibility.Visible : Visibility.Collapsed;
        ResultPage.Visibility = page == AppPage.Result ? Visibility.Visible : Visibility.Collapsed;

        BtnBack.Visibility = page != AppPage.Upload ? Visibility.Visible : Visibility.Collapsed;
        BtnReset.Visibility = page != AppPage.Upload ? Visibility.Visible : Visibility.Collapsed;

        AllowDrop = page == AppPage.Upload;
    }

    private void UpdateExchangeDisplay()
    {
        var data = _viewModel.ExchangeData;
        if (data == null) return;

        TxtDate.Text = data.Date;
        TxtEurUsd.Text = TurkishNumberHelper.FormatRate(data.EurUsdRate);
        TxtSdrUsd.Text = TurkishNumberHelper.FormatRate(data.SdrUsdRate);
    }

    private void UpdateTransportDisplay()
    {
        var isOther = _viewModel.TransportType == TransportType.Other;
        TxtCustomSdr.Visibility = isOther ? Visibility.Visible : Visibility.Collapsed;
        TxtSdrDisplay.Visibility = isOther ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateResultDisplay()
    {
        var r = _viewModel.Result;
        if (r == null) return;

        TxtResKg.Text = TurkishNumberHelper.Format(r.GrossKg);
        TxtResSdr.Text = TurkishNumberHelper.Format(r.SdrConstant);
        TxtResTotalSdr.Text = TurkishNumberHelper.Format(r.SdrAmount);
        TxtResSdrRate.Text = TurkishNumberHelper.FormatRate(r.SdrUsdRate);
        TxtResEurRate.Text = TurkishNumberHelper.FormatRate(r.EurUsdRate);
        TxtResDate.Text = r.EffectiveLoadingDate;
        TxtResSdrEur.Text = $"{TurkishNumberHelper.Format(r.SdrAmountEur)} EUR";
        TxtResAssessment.Text = $"{TurkishNumberHelper.Format(r.AssessmentAmountEur)} EUR";

        // Highlight the active card
        var accentBorder = new SolidColorBrush(Color.FromArgb(0x33, 0x4A, 0xAD, 0xE0));
        var accentBg = new SolidColorBrush(Color.FromArgb(0x0A, 0x4A, 0xAD, 0xE0));
        var defaultBorder = (Brush)FindResource("BorderColor");
        var defaultBg = (Brush)FindResource("SurfaceMuted");

        CardSdrEur.BorderBrush = r.UseSdrLimit ? accentBorder : defaultBorder;
        CardSdrEur.Background = r.UseSdrLimit ? accentBg : defaultBg;
        TxtResSdrEur.Foreground = r.UseSdrLimit ? (Brush)FindResource("AccentColor") : (Brush)FindResource("TextPrimary");

        CardAssessment.BorderBrush = !r.UseSdrLimit ? accentBorder : defaultBorder;
        CardAssessment.Background = !r.UseSdrLimit ? accentBg : defaultBg;
        TxtResAssessment.Foreground = !r.UseSdrLimit ? (Brush)FindResource("AccentColor") : (Brush)FindResource("TextPrimary");

        // Comparison indicator
        if (r.UseSdrLimit)
        {
            ComparisonBorder.Background = new SolidColorBrush(Color.FromArgb(0x1A, 0xF9, 0x73, 0x16));
            ComparisonBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(0x33, 0xF9, 0x73, 0x16));
            ComparisonBorder.BorderThickness = new Thickness(1);
            ComparisonDot.Fill = new SolidColorBrush(Color.FromRgb(0xF9, 0x73, 0x16));
            TxtComparison.Foreground = new SolidColorBrush(Color.FromRgb(0xFB, 0x92, 0x3C));
            TxtComparison.Text = "SDR hesabı tespit tutarından düşük \u2014 SDR tespit tutarı dikkate alınır";
        }
        else
        {
            ComparisonBorder.Background = new SolidColorBrush(Color.FromArgb(0x1A, 0x22, 0xC5, 0x5E));
            ComparisonBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(0x33, 0x22, 0xC5, 0x5E));
            ComparisonBorder.BorderThickness = new Thickness(1);
            ComparisonDot.Fill = new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E));
            TxtComparison.Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80));
            TxtComparison.Text = "SDR hesabı tespit tutarından yüksek \u2014 Tespit tutarı dikkate alınır";
        }

        // Date warning
        DateWarningBorder.Visibility = r.DateWarning == DateWarning.Future
            ? Visibility.Visible : Visibility.Collapsed;

        // Preview text (plain text, not HTML)
        TxtPreview.Text = r.ResultText;
    }

    private void UpdateCopyButton()
    {
        if (_viewModel.Copied)
        {
            BtnCopy.Content = "Kopyalandı!";
            BtnCopy.Style = (Style)FindResource("SuccessButton");
        }
        else
        {
            BtnCopy.Content = "Panoya Kopyala";
            BtnCopy.Style = (Style)FindResource("AccentButton");
        }
    }

    private void BtnInfo_Click(object sender, RoutedEventArgs e)
    {
        new InfoWindow { Owner = this }.ShowDialog();
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
            e.Effects = DragDropEffects.Copy;
        else
            e.Effects = DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            _viewModel.HandleFileDrop(files);
        }
    }
}
