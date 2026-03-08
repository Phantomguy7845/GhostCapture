namespace GhostCapture.App.Models;

public sealed class ConnectionState
{
    public required string Headline { get; init; }

    public required string Detail { get; init; }

    public required string StatusButtonLabel { get; init; }

    public required ConnectionTransport Transport { get; init; }

    public DeviceInfo? ActiveDevice { get; init; }

    public bool HasReadyDevice => ActiveDevice is not null;

    public static ConnectionState FromDevices(IReadOnlyList<DeviceInfo> devices)
    {
        var readyDevices = devices.Where(device => device.IsReady).ToList();
        var preferredReadyDevice = readyDevices
            .OrderBy(device => device.Transport == ConnectionTransport.Usb ? 0 : 1)
            .FirstOrDefault();

        if (preferredReadyDevice is not null)
        {
            var transportLabel = preferredReadyDevice.Transport == ConnectionTransport.Usb ? "USB" : "Wi-Fi";
            var modelLabel = preferredReadyDevice.Model ?? preferredReadyDevice.DeviceName ?? preferredReadyDevice.Serial;

            return new ConnectionState
            {
                Headline = "Device ready",
                Detail = $"{modelLabel} connected over {transportLabel}.",
                StatusButtonLabel = $"{transportLabel} Connected",
                Transport = preferredReadyDevice.Transport,
                ActiveDevice = preferredReadyDevice,
            };
        }

        var unauthorizedDevice = devices.FirstOrDefault(device => string.Equals(device.State, "unauthorized", StringComparison.OrdinalIgnoreCase));
        if (unauthorizedDevice is not null)
        {
            return new ConnectionState
            {
                Headline = "Authorization required",
                Detail = "Approve the USB debugging prompt on the phone, then refresh.",
                StatusButtonLabel = "Waiting Auth",
                Transport = ConnectionTransport.None,
            };
        }

        var offlineDevice = devices.FirstOrDefault(device => string.Equals(device.State, "offline", StringComparison.OrdinalIgnoreCase));
        if (offlineDevice is not null)
        {
            return new ConnectionState
            {
                Headline = "Device offline",
                Detail = "Android was detected but is not ready for mirroring yet.",
                StatusButtonLabel = "Device Offline",
                Transport = ConnectionTransport.None,
            };
        }

        return new ConnectionState
        {
            Headline = "No device connected",
            Detail = "Connect by USB debugging or open Wi-Fi debugging pairing.",
            StatusButtonLabel = "No Device",
            Transport = ConnectionTransport.None,
        };
    }
}

