namespace GhostCapture.App.Models;

public sealed class ConnectionState
{
    public required string Headline { get; init; }

    public required string Detail { get; init; }

    public required string GuidanceTitle { get; init; }

    public required string GuidanceDetail { get; init; }

    public bool HasSupplementalGuidance { get; init; }

    public required string StatusButtonLabel { get; init; }

    public required ConnectionTransport Transport { get; init; }

    public bool NeedsAttention { get; init; }

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
            if (readyDevices.Count > 1)
            {
                var detail = $"{preferredReadyDevice.DisplayName} will open first over {preferredReadyDevice.TransportLabel}.";
                var guidance = readyDevices.Any(device => device.Transport == ConnectionTransport.Usb) &&
                               readyDevices.Any(device => device.Transport == ConnectionTransport.Wireless)
                    ? "GhostCapture mirrors one phone at a time and prefers USB when both USB and Wi-Fi are ready."
                    : "GhostCapture mirrors one phone at a time. Disconnect extra phones if you want a different device.";

                return new ConnectionState
                {
                    Headline = $"{readyDevices.Count} devices ready",
                    Detail = detail,
                    GuidanceTitle = string.Empty,
                    GuidanceDetail = string.Empty,
                    StatusButtonLabel = $"{readyDevices.Count} Ready",
                    Transport = preferredReadyDevice.Transport,
                    ActiveDevice = preferredReadyDevice,
                };
            }

            return new ConnectionState
            {
                Headline = preferredReadyDevice.Transport == ConnectionTransport.Usb ? "USB device ready" : "Wi-Fi device ready",
                Detail = $"{preferredReadyDevice.DisplayName} is ready over {preferredReadyDevice.TransportLabel}.",
                GuidanceTitle = string.Empty,
                GuidanceDetail = string.Empty,
                StatusButtonLabel = $"{preferredReadyDevice.TransportLabel} Connected",
                Transport = preferredReadyDevice.Transport,
                ActiveDevice = preferredReadyDevice,
            };
        }

        var unauthorizedDevice = devices.FirstOrDefault(device => device.IsUnauthorized);
        if (unauthorizedDevice is not null)
        {
            return new ConnectionState
            {
                Headline = "Approve USB debugging",
                Detail = $"{unauthorizedDevice.DisplayName} is waiting for permission on the phone.",
                GuidanceTitle = "On the phone",
                GuidanceDetail = "Unlock Android and tap Allow on the USB debugging prompt.",
                HasSupplementalGuidance = true,
                StatusButtonLabel = "Waiting Auth",
                Transport = ConnectionTransport.None,
                NeedsAttention = true,
            };
        }

        var offlineDevice = devices.FirstOrDefault(device => device.IsOffline);
        if (offlineDevice is not null)
        {
            return new ConnectionState
            {
                Headline = "Device found, not ready yet",
                Detail = $"{offlineDevice.DisplayName} is visible to ADB but still offline.",
                GuidanceTitle = "Quick fix",
                GuidanceDetail = "Reconnect the cable or toggle debugging if this state does not clear.",
                HasSupplementalGuidance = true,
                StatusButtonLabel = "Device Offline",
                Transport = ConnectionTransport.None,
                NeedsAttention = true,
            };
        }

        var otherDevice = devices.FirstOrDefault();
        if (otherDevice is not null)
        {
            return new ConnectionState
            {
                Headline = "Device detected",
                Detail = $"{otherDevice.DisplayName} is reporting '{otherDevice.State}'.",
                GuidanceTitle = "Next step",
                GuidanceDetail = "Refresh after the phone finishes connecting, or reopen USB/Wireless debugging.",
                HasSupplementalGuidance = true,
                StatusButtonLabel = "Device Found",
                Transport = ConnectionTransport.None,
                NeedsAttention = true,
            };
        }

        return new ConnectionState
        {
            Headline = "No device connected",
            Detail = "Plug in USB or open Wi-Fi pairing.",
            GuidanceTitle = string.Empty,
            GuidanceDetail = string.Empty,
            StatusButtonLabel = "No Device",
            Transport = ConnectionTransport.None,
        };
    }
}
