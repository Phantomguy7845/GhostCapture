using System.Text.RegularExpressions;
using GhostCapture.App.Models;

namespace GhostCapture.App.Services;

public sealed class AdbService : IAdbService
{
    private const int GhostCaptureAdbServerPort = 5038;
    private static readonly Regex MdnsLinePattern = new(
        @"^(?<name>\S+)\s+(?<type>_[^\s]+)\s+(?<endpoint>\S+)$",
        RegexOptions.Compiled);
    private static readonly IReadOnlyDictionary<string, string> AdbEnvironment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["ADB_MDNS_OPENSCREEN"] = "1",
        ["ANDROID_ADB_SERVER_PORT"] = GhostCaptureAdbServerPort.ToString(),
    };

    private readonly ProcessRunner _processRunner;
    private readonly ToolPathResolver _toolPathResolver;

    public AdbService(ProcessRunner processRunner, ToolPathResolver toolPathResolver)
    {
        _processRunner = processRunner;
        _toolPathResolver = toolPathResolver;
    }

    public async Task<IReadOnlyList<DeviceInfo>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        var result = await _processRunner.RunAsync(
            _toolPathResolver.GetAdbPath(),
            BuildArguments("devices -l"),
            _toolPathResolver.GetWorkingDirectory(),
            AdbEnvironment,
            cancellationToken);

        var devices = new List<DeviceInfo>();

        foreach (var rawLine in result.StandardOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("List of devices attached", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 2; index < parts.Length; index++)
            {
                var separatorIndex = parts[index].IndexOf(':');
                if (separatorIndex <= 0 || separatorIndex == parts[index].Length - 1)
                {
                    continue;
                }

                metadata[parts[index][..separatorIndex]] = parts[index][(separatorIndex + 1)..];
            }

            var serial = parts[0];
            devices.Add(new DeviceInfo
            {
                Serial = serial,
                State = parts[1],
                Product = metadata.GetValueOrDefault("product"),
                Model = metadata.GetValueOrDefault("model"),
                DeviceName = metadata.GetValueOrDefault("device"),
                Transport = DetectTransport(serial, metadata),
            });
        }

        return devices;
    }

    public async Task<IReadOnlyList<MdnsServiceInfo>> GetMdnsServicesAsync(CancellationToken cancellationToken = default)
    {
        var result = await _processRunner.RunAsync(
            _toolPathResolver.GetAdbPath(),
            BuildArguments("mdns services"),
            _toolPathResolver.GetWorkingDirectory(),
            AdbEnvironment,
            cancellationToken);

        var services = new List<MdnsServiceInfo>();
        foreach (var rawLine in result.StandardOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("List of discovered", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var match = MdnsLinePattern.Match(line);
            if (!match.Success)
            {
                continue;
            }

            services.Add(new MdnsServiceInfo
            {
                InstanceName = match.Groups["name"].Value,
                ServiceType = match.Groups["type"].Value,
                Endpoint = match.Groups["endpoint"].Value,
            });
        }

        return services;
    }

    public Task<ProcessResult> PairAsync(string endpoint, string secret, CancellationToken cancellationToken = default)
    {
        var arguments = BuildArguments($"pair {endpoint} {secret}");
        return _processRunner.RunAsync(
            _toolPathResolver.GetAdbPath(),
            arguments,
            _toolPathResolver.GetWorkingDirectory(),
            AdbEnvironment,
            cancellationToken);
    }

    public Task<ProcessResult> ConnectAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var arguments = BuildArguments($"connect {endpoint}");
        return _processRunner.RunAsync(
            _toolPathResolver.GetAdbPath(),
            arguments,
            _toolPathResolver.GetWorkingDirectory(),
            AdbEnvironment,
            cancellationToken);
    }

    private static ConnectionTransport DetectTransport(string serial, IReadOnlyDictionary<string, string> metadata)
    {
        if (metadata.ContainsKey("usb"))
        {
            return ConnectionTransport.Usb;
        }

        if (serial.Contains(':', StringComparison.Ordinal) ||
            serial.Contains("._adb-tls-connect._tcp", StringComparison.OrdinalIgnoreCase) ||
            serial.Contains("._adb._tcp", StringComparison.OrdinalIgnoreCase))
        {
            return ConnectionTransport.Wireless;
        }

        return ConnectionTransport.Usb;
    }

    private static string BuildArguments(string arguments)
    {
        return $"-P {GhostCaptureAdbServerPort} {arguments}";
    }
}
