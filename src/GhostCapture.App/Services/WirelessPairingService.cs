using System.Security.Cryptography;
using System.Text;
using GhostCapture.App.Models;

namespace GhostCapture.App.Services;

public sealed class WirelessPairingService : IWirelessPairingService
{
    private readonly IAdbService _adbService;

    public WirelessPairingService(IAdbService adbService)
    {
        _adbService = adbService;
    }

    public WirelessPairingSession CreateSession()
    {
        var serviceName = $"studio-{GenerateToken(10)}";
        var secret = GenerateToken(12);
        var session = new WirelessPairingSession
        {
            ServiceName = serviceName,
            Secret = secret,
            QrPayload = $"WIFI:T:ADB;S:{serviceName};P:{secret};;",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        DiagnosticLog.Write("pairing", $"Created Wi-Fi pairing session '{serviceName}'.");
        return session;
    }

    public async Task<WirelessPairingResult> PairAndConnectAsync(
        WirelessPairingSession session,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Report(progress, "Waiting for Android to publish the pairing service after the QR scan...");
        DiagnosticLog.Write("pairing", $"Waiting for pairing service '{session.ServiceName}'.");

        var pairingService = await WaitForServiceAsync(
            session.ServiceName,
            "_adb-tls-pairing._tcp",
            TimeSpan.FromMinutes(2),
            progress,
            cancellationToken);
        if (pairingService is null)
        {
            DiagnosticLog.Write("pairing", $"Pairing service '{session.ServiceName}' was not discovered before timeout.");
            return new WirelessPairingResult
            {
                Message = "Timed out waiting for the phone to scan the Wi-Fi debugging QR code.",
            };
        }

        Report(progress, $"QR scan detected. Pairing with {pairingService.Endpoint}...");
        DiagnosticLog.Write("pairing", $"Discovered pairing service '{pairingService.InstanceName}' at {pairingService.Endpoint}.");

        var pairingResult = await _adbService.PairAsync(pairingService.Endpoint, session.Secret, cancellationToken);
        DiagnosticLog.Write(
            "pairing",
            $"adb pair {pairingService.Endpoint} exit={pairingResult.ExitCode} stdout='{SanitizeForLog(pairingResult.StandardOutput)}' stderr='{SanitizeForLog(pairingResult.StandardError)}'.");

        if (!pairingResult.Succeeded)
        {
            return new WirelessPairingResult
            {
                Message = BuildAdbFailureMessage(pairingResult, "adb pair failed."),
            };
        }

        Report(progress, "Pairing accepted. Waiting for the wireless device to come online...");
        var readyDevice = await WaitForWirelessDeviceAsync(TimeSpan.FromSeconds(20), progress, cancellationToken);
        if (readyDevice is not null)
        {
            DiagnosticLog.Write("pairing", $"Wireless device became ready without fallback connect: {readyDevice.Serial}.");
            return new WirelessPairingResult
            {
                IsSuccess = true,
                Message = "Wireless debugging connected.",
                Device = readyDevice,
            };
        }

        Report(progress, "Pairing accepted. Looking for the secure wireless connect service...");
        var connectService = await WaitForMatchingConnectServiceAsync(pairingService.Endpoint, TimeSpan.FromSeconds(10), progress, cancellationToken);
        if (connectService is not null)
        {
            Report(progress, $"Requesting ADB wireless connection to {connectService.Endpoint}...");
            DiagnosticLog.Write("pairing", $"Discovered connect service '{connectService.InstanceName}' at {connectService.Endpoint}.");

            var connectResult = await _adbService.ConnectAsync(connectService.Endpoint, cancellationToken);
            DiagnosticLog.Write(
                "pairing",
                $"adb connect {connectService.Endpoint} exit={connectResult.ExitCode} stdout='{SanitizeForLog(connectResult.StandardOutput)}' stderr='{SanitizeForLog(connectResult.StandardError)}'.");

            readyDevice = await WaitForWirelessDeviceAsync(TimeSpan.FromSeconds(15), progress, cancellationToken);
        }

        DiagnosticLog.Write(
            "pairing",
            readyDevice is not null
                ? $"Wireless pairing finished with ready device {readyDevice.Serial}."
                : "Wireless pairing finished without a ready wireless device.");

        return new WirelessPairingResult
        {
            IsSuccess = readyDevice is not null,
            Message = readyDevice is not null
                ? "Wireless debugging connected."
                : "Pairing succeeded but no wireless device became ready yet.",
            Device = readyDevice,
        };
    }

    private async Task<MdnsServiceInfo?> WaitForServiceAsync(
        string instanceName,
        string serviceType,
        TimeSpan timeout,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var timeoutAt = DateTimeOffset.UtcNow.Add(timeout);
        var attempt = 0;

        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            attempt++;
            var services = await _adbService.GetMdnsServicesAsync(cancellationToken);
            var match = services.FirstOrDefault(service =>
                string.Equals(service.InstanceName, instanceName, StringComparison.Ordinal) &&
                string.Equals(service.ServiceType, serviceType, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                return match;
            }

            if (attempt == 1 || attempt % 5 == 0)
            {
                var visibleServices = services.Count == 0
                    ? "none"
                    : string.Join(", ", services.Select(service => $"{service.InstanceName} {service.ServiceType} {service.Endpoint}"));

                Report(progress, $"Still waiting for the phone to scan the QR code. Visible mDNS services: {visibleServices}.");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        return null;
    }

    private async Task<DeviceInfo?> WaitForWirelessDeviceAsync(
        TimeSpan timeout,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var timeoutAt = DateTimeOffset.UtcNow.Add(timeout);
        var attempt = 0;

        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            attempt++;
            var devices = await _adbService.GetDevicesAsync(cancellationToken);
            var readyWirelessDevice = devices.FirstOrDefault(device => device.IsReady && device.Transport == ConnectionTransport.Wireless);
            if (readyWirelessDevice is not null)
            {
                return readyWirelessDevice;
            }

            if (attempt == 1 || attempt % 5 == 0)
            {
                var visibleDevices = devices.Count == 0
                    ? "none"
                    : string.Join(", ", devices.Select(device => $"{device.Serial} [{device.State}/{device.Transport}]"));

                Report(progress, $"Still waiting for the wireless device to come online. Visible devices: {visibleDevices}.");
                DiagnosticLog.Write("pairing", $"Waiting for wireless device. Visible devices: {visibleDevices}.");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        return null;
    }

    private async Task<MdnsServiceInfo?> WaitForMatchingConnectServiceAsync(
        string pairingEndpoint,
        TimeSpan timeout,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var pairingHost = pairingEndpoint.Split(':', 2)[0];
        var timeoutAt = DateTimeOffset.UtcNow.Add(timeout);
        var attempt = 0;

        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            attempt++;
            var services = await _adbService.GetMdnsServicesAsync(cancellationToken);
            var connectService = services.FirstOrDefault(service =>
                string.Equals(service.ServiceType, "_adb-tls-connect._tcp", StringComparison.OrdinalIgnoreCase) &&
                service.Endpoint.StartsWith($"{pairingHost}:", StringComparison.OrdinalIgnoreCase));

            if (connectService is not null)
            {
                return connectService;
            }

            if (attempt == 1 || attempt % 5 == 0)
            {
                var visibleServices = services.Count == 0
                    ? "none"
                    : string.Join(", ", services.Select(service => $"{service.InstanceName} {service.ServiceType} {service.Endpoint}"));

                Report(progress, $"Waiting for the secure wireless connect service. Visible mDNS services: {visibleServices}.");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        return null;
    }

    private static string BuildAdbFailureMessage(ProcessResult result, string fallbackMessage)
    {
        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            return result.StandardError.Trim();
        }

        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            return result.StandardOutput.Trim();
        }

        return fallbackMessage;
    }

    private static void Report(IProgress<string>? progress, string message)
    {
        progress?.Report(message);
        DiagnosticLog.Write("pairing", message);
    }

    private static string SanitizeForLog(string value)
    {
        return value.Replace(Environment.NewLine, " ").Trim();
    }

    private static string GenerateToken(int length)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
        var bytes = RandomNumberGenerator.GetBytes(length);
        var builder = new StringBuilder(length);

        foreach (var value in bytes)
        {
            builder.Append(alphabet[value % alphabet.Length]);
        }

        return builder.ToString();
    }
}
