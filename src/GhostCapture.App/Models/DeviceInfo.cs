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
}

