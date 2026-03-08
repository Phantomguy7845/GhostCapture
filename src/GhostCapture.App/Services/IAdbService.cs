using GhostCapture.App.Models;

namespace GhostCapture.App.Services;

public interface IAdbService
{
    Task<IReadOnlyList<DeviceInfo>> GetDevicesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MdnsServiceInfo>> GetMdnsServicesAsync(CancellationToken cancellationToken = default);

    Task<ProcessResult> PairAsync(string endpoint, string secret, CancellationToken cancellationToken = default);

    Task<ProcessResult> ConnectAsync(string endpoint, CancellationToken cancellationToken = default);
}

