using GhostCapture.App.Infrastructure;
using GhostCapture.App.Models;
using GhostCapture.App.Services;
using System.IO;
using System.Windows.Media.Imaging;

namespace GhostCapture.App.ViewModels;

public sealed class PairingViewModel : ObservableObject
{
    private readonly IWirelessPairingService _wirelessPairingService;
    private readonly IScrcpyService _scrcpyService;
    private readonly IQrCodeImageService _qrCodeImageService;
    private readonly Func<Task> _refreshMainWindowAsync;
    private CancellationTokenSource? _pairingCancellationTokenSource;
    private WirelessPairingSession _session;
    private BitmapSource? _qrCodeImage;
    private string _statusMessage = "Open Wireless debugging on Android, then scan this QR code.";
    private bool _isBusy;

    public PairingViewModel(
        IWirelessPairingService wirelessPairingService,
        IScrcpyService scrcpyService,
        IQrCodeImageService qrCodeImageService,
        Func<Task> refreshMainWindowAsync)
    {
        _wirelessPairingService = wirelessPairingService;
        _scrcpyService = scrcpyService;
        _qrCodeImageService = qrCodeImageService;
        _refreshMainWindowAsync = refreshMainWindowAsync;
        _session = _wirelessPairingService.CreateSession();
        _qrCodeImage = _qrCodeImageService.GenerateQrCode(_session.QrPayload);
    }

    public event EventHandler? RequestClose;

    public string ServiceName => _session.ServiceName;

    public string Secret => _session.Secret;

    public string QrPayload => _session.QrPayload;

    public BitmapSource? QrCodeImage
    {
        get => _qrCodeImage;
        private set => SetProperty(ref _qrCodeImage, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsIdle));
            }
        }
    }

    public bool IsIdle => !IsBusy;

    public void RegenerateSession()
    {
        if (IsBusy)
        {
            return;
        }

        _session = _wirelessPairingService.CreateSession();
        QrCodeImage = _qrCodeImageService.GenerateQrCode(_session.QrPayload);
        StatusMessage = "New Wi-Fi QR ready. Scan it from Android.";
        OnPropertyChanged(nameof(ServiceName));
        OnPropertyChanged(nameof(Secret));
        OnPropertyChanged(nameof(QrPayload));
    }

    public void CancelPairing()
    {
        _pairingCancellationTokenSource?.Cancel();
    }

    public async Task BeginPairingAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        _pairingCancellationTokenSource?.Dispose();
        _pairingCancellationTokenSource = new CancellationTokenSource();
        var progress = new Progress<string>(message => StatusMessage = message);

        try
        {
            var result = await _wirelessPairingService.PairAndConnectAsync(_session, progress, _pairingCancellationTokenSource.Token);
            StatusMessage = result.Message;

            if (result.IsSuccess && result.Device is not null)
            {
                await _refreshMainWindowAsync();
                await _scrcpyService.LaunchAsync(result.Device, ScrcpyLaunchProfile.CompetitiveLowLatency());
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Wi-Fi pairing was cancelled.";
        }
        catch (Exception exception)
        {
            StatusMessage = exception is FileNotFoundException
                ? "GhostCapture cannot find its bundled adb or scrcpy files."
                : "Wi-Fi pairing could not continue right now. Try a new QR code.";
        }
        finally
        {
            _pairingCancellationTokenSource?.Dispose();
            _pairingCancellationTokenSource = null;
            IsBusy = false;
        }
    }
}
