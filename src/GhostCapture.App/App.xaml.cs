using System.IO;
using System.Windows;
using System.Windows.Threading;
using GhostCapture.App.Services;
using GhostCapture.App.ViewModels;
using GhostCapture.App.Views;

namespace GhostCapture.App;

public partial class App : Application
{
    private readonly string _logDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GhostCapture",
        "logs");

    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var toolPathResolver = new ToolPathResolver();
        var processRunner = new ProcessRunner();
        var adbService = new AdbService(processRunner, toolPathResolver);
        var scrcpyService = new ScrcpyService(processRunner, toolPathResolver);
        var wirelessPairingService = new WirelessPairingService(adbService);
        var qrCodeImageService = new QrCodeImageService();

        var mainViewModel = new MainViewModel(adbService, scrcpyService, wirelessPairingService, qrCodeImageService);
        var mainWindow = new MainWindow
        {
            DataContext = mainViewModel,
        };

        MainWindow = mainWindow;
        mainWindow.Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WriteExceptionLog("dispatcher", e.Exception);
        MessageBox.Show(
            $"GhostCapture hit an unexpected error.\n\n{e.Exception.Message}\n\nA log was written to:\n{_logDirectory}",
            "GhostCapture Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            WriteExceptionLog("appdomain", exception);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        WriteExceptionLog("task", e.Exception);
        e.SetObserved();
    }

    private void WriteExceptionLog(string channel, Exception exception)
    {
        Directory.CreateDirectory(_logDirectory);
        var fileName = $"ghostcapture-{DateTime.Now:yyyyMMdd-HHmmss}-{channel}.log";
        var fullPath = Path.Combine(_logDirectory, fileName);

        var content = $"""
        Timestamp: {DateTime.Now:O}
        Channel: {channel}
        Message: {exception.Message}

        {exception}
        """;

        File.WriteAllText(fullPath, content);
    }
}
