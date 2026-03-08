namespace GhostCapture.App.Models;

public sealed class WirelessPairingSession
{
    public required string ServiceName { get; init; }

    public required string Secret { get; init; }

    public required string QrPayload { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}

