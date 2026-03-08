using GhostCapture.App.Models;

namespace GhostCapture.App.Services;

public interface IScrcpyService
{
    Task LaunchAsync(DeviceInfo device, ScrcpyLaunchProfile profile, CancellationToken cancellationToken = default);
}

