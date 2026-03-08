using GhostCapture.App.Models;

namespace GhostCapture.App.Services;

public interface IWirelessPairingService
{
    WirelessPairingSession CreateSession();

    Task<WirelessPairingResult> PairAndConnectAsync(
        WirelessPairingSession session,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
