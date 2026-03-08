using GhostCapture.App.Commands;
using GhostCapture.App.Infrastructure;
using GhostCapture.App.Models;
using GhostCapture.App.Services;
using System.Windows.Input;

namespace GhostCapture.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly IAdbService _adbService;
    private readonly IScrcpyService _scrcpyService;
    private readonly IWirelessPairingService _wirelessPairingService;
    private readonly IQrCodeImageService _qrCodeImageService;
    private ConnectionState _connectionState = ConnectionState.FromDevices(Array.Empty<DeviceInfo>());
    private string _errorMessage = string.Empty;
    private bool _isBusy;

    public MainViewModel(
        IAdbService adbService,
        IScrcpyService scrcpyService,
        IWirelessPairingService wirelessPairingService,
        IQrCodeImageService qrCodeImageService)
    {
        _adbService = adbService;
        _scrcpyService = scrcpyService;
        _wirelessPairingService = wirelessPairingService;
        _qrCodeImageService = qrCodeImageService;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        StartScreenCommand = new AsyncRelayCommand(StartScreenAsync, () => !IsBusy);
    }

    public ICommand RefreshCommand { get; }

    public ICommand StartScreenCommand { get; }

    public string ConnectionHeadline => _connectionState.Headline;

    public string ConnectionDetail => _connectionState.Detail;

    public string StatusButtonLabel => _connectionState.StatusButtonLabel;

    public string StartButtonLabel => HasReadyDevice ? "Start Screen" : "Waiting For Device";

    public bool HasReadyDevice => _connectionState.HasReadyDevice;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsIdle));
                RefreshCommandState();
            }
        }
    }

    public bool IsIdle => !IsBusy;

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var devices = await _adbService.GetDevicesAsync();
            _connectionState = ConnectionState.FromDevices(devices);
            NotifyConnectionStateChanged();
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task StartScreenAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (_connectionState.ActiveDevice is null)
        {
            ErrorMessage = "No ready Android device is available.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            await _scrcpyService.LaunchAsync(_connectionState.ActiveDevice, ScrcpyLaunchProfile.CompetitiveLowLatency());
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public PairingViewModel CreatePairingViewModel()
    {
        return new PairingViewModel(_wirelessPairingService, _scrcpyService, _qrCodeImageService, RefreshAsync);
    }

    private void NotifyConnectionStateChanged()
    {
        OnPropertyChanged(nameof(ConnectionHeadline));
        OnPropertyChanged(nameof(ConnectionDetail));
        OnPropertyChanged(nameof(StatusButtonLabel));
        OnPropertyChanged(nameof(StartButtonLabel));
        OnPropertyChanged(nameof(HasReadyDevice));
    }

    private void RefreshCommandState()
    {
        if (RefreshCommand is AsyncRelayCommand refreshCommand)
        {
            refreshCommand.RaiseCanExecuteChanged();
        }

        if (StartScreenCommand is AsyncRelayCommand startScreenCommand)
        {
            startScreenCommand.RaiseCanExecuteChanged();
        }
    }
}
