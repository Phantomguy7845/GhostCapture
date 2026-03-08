namespace GhostCapture.App.Models;

public sealed class DeviceInfo
{
    public required string Serial { get; init; }

    public required string State { get; init; }

    public string? Model { get; init; }

    public string? Product { get; init; }

    public string? DeviceName { get; init; }

    public ConnectionTransport Transport { get; init; }

    public bool IsReady => string.Equals(State, "device", StringComparison.OrdinalIgnoreCase);

    public bool IsUnauthorized => string.Equals(State, "unauthorized", StringComparison.OrdinalIgnoreCase);

    public bool IsOffline => string.Equals(State, "offline", StringComparison.OrdinalIgnoreCase);

    public string DisplayName
    {
        get
        {
            var preferred = Model ?? DeviceName ?? Product ?? Serial;
            return preferred.Replace('_', ' ');
        }
    }

    public string TransportLabel => Transport switch
    {
        ConnectionTransport.Usb => "USB",
        ConnectionTransport.Wireless => "Wi-Fi",
        _ => "Unknown",
    };
}
