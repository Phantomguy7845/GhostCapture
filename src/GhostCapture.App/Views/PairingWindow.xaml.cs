using System.Windows;
using GhostCapture.App.ViewModels;

namespace GhostCapture.App.Views;

public partial class PairingWindow : Window
{
    private readonly PairingViewModel _viewModel;

    public PairingWindow(PairingViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _viewModel.RequestClose += ViewModelOnRequestClose;
        DataContext = _viewModel;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.BeginPairingAsync();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.CancelPairing();
        _viewModel.RequestClose -= ViewModelOnRequestClose;
    }

    private void ViewModelOnRequestClose(object? sender, EventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CopyPayload_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(_viewModel.QrPayload);
    }

    private async void Retry_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.RegenerateSession();
        await _viewModel.BeginPairingAsync();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
