using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using GhostCapture.App.ViewModels;

namespace GhostCapture.App.Views;

public partial class MainWindow : Window
{
    private const double WifiMorphTargetWidth = 430;
    private const double WifiMorphTargetHeight = 486;

    private readonly DispatcherTimer _refreshTimer;
    private PairingViewModel? _pairingViewModel;
    private bool _isWifiOverlayOpen;
    private bool _isRefreshing;

    public MainWindow()
    {
        InitializeComponent();

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2),
        };

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _refreshTimer.Tick += RefreshTimerOnTick;
        _refreshTimer.Start();

        if (DataContext is MainViewModel viewModel)
        {
            await viewModel.RefreshAsync();
            if (viewModel.HasReadyDevice)
            {
                await viewModel.StartScreenAsync();
            }
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _refreshTimer.Stop();
        _refreshTimer.Tick -= RefreshTimerOnTick;
        DetachPairingViewModel(cancelPairing: true);
    }

    private async void RefreshTimerOnTick(object? sender, EventArgs e)
    {
        if (_isRefreshing || DataContext is not MainViewModel viewModel)
        {
            return;
        }

        _isRefreshing = true;
        try
        {
            await viewModel.RefreshAsync();
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private async void RefreshConnection_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            await viewModel.RefreshAsync();
        }
    }

    private async void OpenWifiPairing_Click(object sender, RoutedEventArgs e)
    {
        if (_isWifiOverlayOpen || DataContext is not MainViewModel viewModel)
        {
            return;
        }

        DetachPairingViewModel(cancelPairing: true);

        _pairingViewModel = viewModel.CreatePairingViewModel();
        _pairingViewModel.RequestClose += PairingViewModelOnRequestClose;

        WifiMorphCard.DataContext = _pairingViewModel;
        WifiMorphCompactContent.Opacity = 1;
        WifiMorphExpandedContent.Opacity = 0;

        var startRect = GetElementBounds(WifiLauncherCard, RootSurface);
        var targetRect = GetWifiMorphTargetRect();

        SetMorphCardBounds(startRect);

        WifiMorphLayer.Visibility = Visibility.Visible;
        WifiMorphLayer.IsHitTestVisible = true;
        WifiMorphCard.Visibility = Visibility.Visible;
        Panel.SetZIndex(WifiMorphCard, 1);

        _isWifiOverlayOpen = true;

        await Task.WhenAll(
            RunStoryboardAsync("OpenWifiMorphScene"),
            AnimateWifiMorphOpenAsync(startRect, targetRect));

        if (_isWifiOverlayOpen && _pairingViewModel is not null)
        {
            await _pairingViewModel.BeginPairingAsync();
        }
    }

    private async void PairingViewModelOnRequestClose(object? sender, EventArgs e)
    {
        await CloseWifiOverlayAsync(cancelPairing: false);
    }

    private void CopyWifiPayload_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_pairingViewModel?.QrPayload))
        {
            Clipboard.SetText(_pairingViewModel.QrPayload);
        }
    }

    private async void RegenerateWifiPairing_Click(object sender, RoutedEventArgs e)
    {
        if (_pairingViewModel is null)
        {
            return;
        }

        _pairingViewModel.RegenerateSession();
        await _pairingViewModel.BeginPairingAsync();
    }

    private async void CloseWifiOverlay_Click(object sender, RoutedEventArgs e)
    {
        await CloseWifiOverlayAsync(cancelPairing: true);
    }

    private async Task CloseWifiOverlayAsync(bool cancelPairing)
    {
        if (!_isWifiOverlayOpen)
        {
            return;
        }

        _isWifiOverlayOpen = false;

        if (cancelPairing)
        {
            _pairingViewModel?.CancelPairing();
        }

        var returnRect = GetElementBounds(WifiLauncherCard, RootSurface);

        await Task.WhenAll(
            RunStoryboardAsync("CloseWifiMorphScene"),
            AnimateWifiMorphCloseAsync(returnRect));

        WifiMorphCard.Visibility = Visibility.Collapsed;
        WifiMorphLayer.IsHitTestVisible = false;
        WifiMorphLayer.Visibility = Visibility.Collapsed;
        WifiMorphCard.DataContext = null;

        DetachPairingViewModel(cancelPairing: false);

        if (DataContext is MainViewModel viewModel)
        {
            await viewModel.RefreshAsync();
        }
    }

    private void DetachPairingViewModel(bool cancelPairing)
    {
        if (_pairingViewModel is null)
        {
            return;
        }

        if (cancelPairing)
        {
            _pairingViewModel.CancelPairing();
        }

        _pairingViewModel.RequestClose -= PairingViewModelOnRequestClose;
        _pairingViewModel = null;
    }

    private Task AnimateWifiMorphOpenAsync(Rect startRect, Rect targetRect)
    {
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

        return Task.WhenAll(
            AnimateDoubleAsync(WifiMorphCard, Canvas.LeftProperty, startRect.Left, targetRect.Left, 260, easing),
            AnimateDoubleAsync(WifiMorphCard, Canvas.TopProperty, startRect.Top, targetRect.Top, 260, easing),
            AnimateDoubleAsync(WifiMorphCard, WidthProperty, startRect.Width, targetRect.Width, 280, easing),
            AnimateDoubleAsync(WifiMorphCard, HeightProperty, startRect.Height, targetRect.Height, 280, easing),
            AnimateDoubleAsync(WifiMorphCompactContent, OpacityProperty, 1, 0, 120, easing, 60),
            AnimateDoubleAsync(WifiMorphExpandedContent, OpacityProperty, 0, 1, 180, easing, 110));
    }

    private Task AnimateWifiMorphCloseAsync(Rect returnRect)
    {
        var currentRect = new Rect(
            Canvas.GetLeft(WifiMorphCard),
            Canvas.GetTop(WifiMorphCard),
            WifiMorphCard.Width,
            WifiMorphCard.Height);

        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

        return Task.WhenAll(
            AnimateDoubleAsync(WifiMorphCard, Canvas.LeftProperty, currentRect.Left, returnRect.Left, 240, easing),
            AnimateDoubleAsync(WifiMorphCard, Canvas.TopProperty, currentRect.Top, returnRect.Top, 240, easing),
            AnimateDoubleAsync(WifiMorphCard, WidthProperty, currentRect.Width, returnRect.Width, 240, easing),
            AnimateDoubleAsync(WifiMorphCard, HeightProperty, currentRect.Height, returnRect.Height, 240, easing),
            AnimateDoubleAsync(WifiMorphExpandedContent, OpacityProperty, 1, 0, 100, easing),
            AnimateDoubleAsync(WifiMorphCompactContent, OpacityProperty, 0, 1, 130, easing, 70));
    }

    private Rect GetWifiMorphTargetRect()
    {
        var targetWidth = Math.Min(WifiMorphTargetWidth, Math.Max(360, RootSurface.ActualWidth - 140));
        var targetHeight = Math.Min(WifiMorphTargetHeight, Math.Max(400, RootSurface.ActualHeight - 72));

        var left = (RootSurface.ActualWidth - targetWidth) / 2;
        var top = Math.Max(14, (RootSurface.ActualHeight - targetHeight) / 2);

        return new Rect(left, top, targetWidth, targetHeight);
    }

    private static Rect GetElementBounds(FrameworkElement element, UIElement relativeTo)
    {
        var topLeft = element.TranslatePoint(new Point(0, 0), relativeTo);
        return new Rect(topLeft.X, topLeft.Y, element.ActualWidth, element.ActualHeight);
    }

    private void SetMorphCardBounds(Rect rect)
    {
        WifiMorphCard.Width = rect.Width;
        WifiMorphCard.Height = rect.Height;
        Canvas.SetLeft(WifiMorphCard, rect.Left);
        Canvas.SetTop(WifiMorphCard, rect.Top);
    }

    private static Task AnimateDoubleAsync(
        DependencyObject target,
        DependencyProperty property,
        double from,
        double to,
        int durationMs,
        IEasingFunction easing,
        int beginTimeMs = 0)
    {
        var completionSource = new TaskCompletionSource<object?>();
        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            BeginTime = TimeSpan.FromMilliseconds(beginTimeMs),
            EasingFunction = easing,
            FillBehavior = FillBehavior.HoldEnd,
        };

        animation.Completed += (_, _) => completionSource.TrySetResult(null);

        if (target is IAnimatable animatable)
        {
            animatable.BeginAnimation(property, animation);
        }
        else
        {
            completionSource.TrySetResult(null);
        }

        return completionSource.Task;
    }

    private Task RunStoryboardAsync(string resourceKey)
    {
        var storyboard = ((Storyboard)FindResource(resourceKey)).Clone();
        var completionSource = new TaskCompletionSource<object?>();

        void OnCompleted(object? sender, EventArgs e)
        {
            storyboard.Completed -= OnCompleted;
            completionSource.TrySetResult(null);
        }

        storyboard.Completed += OnCompleted;
        storyboard.Begin(this, true);
        return completionSource.Task;
    }
}
