namespace GhostCapture.App.Models;

public sealed class WirelessPairingResult
{
    public bool IsSuccess { get; init; }

    public string Message { get; init; } = string.Empty;

    public DeviceInfo? Device { get; init; }
}
