using System.Diagnostics;
using GhostCapture.App.Models;

namespace GhostCapture.App.Services;

public sealed class ProcessRunner
{
    public async Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        CancellationToken cancellationToken = default)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory ?? string.Empty,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        ApplyEnvironmentVariables(process.StartInfo, environmentVariables);

        process.Start();

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = await standardOutputTask,
            StandardError = await standardErrorTask,
        };
    }

    public Task StartDetachedAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string>? environmentVariables = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        ApplyEnvironmentVariables(startInfo, environmentVariables);
        Process.Start(startInfo);
        return Task.CompletedTask;
    }

    private static void ApplyEnvironmentVariables(
        ProcessStartInfo startInfo,
        IReadOnlyDictionary<string, string>? environmentVariables)
    {
        if (environmentVariables is null)
        {
            return;
        }

        foreach (var pair in environmentVariables)
        {
            startInfo.Environment[pair.Key] = pair.Value;
        }
    }
}
