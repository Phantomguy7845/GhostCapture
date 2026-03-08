using GhostCapture.App.Models;
using System.Globalization;

namespace GhostCapture.App.Services;

public sealed class ScrcpyService : IScrcpyService
{
    private static readonly IReadOnlyDictionary<string, string> ScrcpyEnvironment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["ADB_MDNS_OPENSCREEN"] = "1",
        ["ANDROID_ADB_SERVER_PORT"] = "5038",
    };

    private readonly ProcessRunner _processRunner;
    private readonly ToolPathResolver _toolPathResolver;

    public ScrcpyService(ProcessRunner processRunner, ToolPathResolver toolPathResolver)
    {
        _processRunner = processRunner;
        _toolPathResolver = toolPathResolver;
    }

    public Task LaunchAsync(DeviceInfo device, ScrcpyLaunchProfile profile, CancellationToken cancellationToken = default)
    {
        var arguments = profile.Arguments
            .Concat(new[] { $"--serial={device.Serial}" })
            .ToArray();

        var environment = new Dictionary<string, string>(ScrcpyEnvironment, StringComparer.OrdinalIgnoreCase)
        {
            ["ADB"] = _toolPathResolver.GetAdbPath(),
        };

        return _processRunner.StartDetachedAsync(
            "wscript.exe",
            BuildWscriptArguments(_toolPathResolver.GetScrcpyNoConsoleLauncherPath(), arguments),
            _toolPathResolver.GetWorkingDirectory(),
            environment);
    }

    private static string BuildWscriptArguments(string scriptPath, IReadOnlyList<string> arguments)
    {
        var escapedArguments = arguments
            .Select(argument => Quote(argument));

        return string.Format(
            CultureInfo.InvariantCulture,
            "//B //NoLogo {0} {1}",
            Quote(scriptPath),
            string.Join(' ', escapedArguments));
    }

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
