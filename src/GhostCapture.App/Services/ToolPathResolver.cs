using System.IO;

namespace GhostCapture.App.Services;

public sealed class ToolPathResolver
{
    private static readonly string[] CandidateDirectories =
    {
        @"tools\scrcpy",
        "scrcpy-win64-v3.3.4",
    };

    public string GetAdbPath()
    {
        return FindToolPath("adb.exe");
    }

    public string GetScrcpyPath()
    {
        return FindToolPath("scrcpy.exe");
    }

    public string GetScrcpyNoConsoleLauncherPath()
    {
        return FindToolPath("scrcpy-noconsole.vbs");
    }

    public string GetWorkingDirectory()
    {
        return Path.GetDirectoryName(GetScrcpyPath()) ?? AppContext.BaseDirectory;
    }

    private static string FindToolPath(string fileName)
    {
        var overrideDirectory = Environment.GetEnvironmentVariable("GHOSTCAPTURE_TOOL_DIR");
        if (!string.IsNullOrWhiteSpace(overrideDirectory))
        {
            var overridePath = Path.Combine(overrideDirectory, fileName);
            if (File.Exists(overridePath))
            {
                return overridePath;
            }
        }

        var localAppPath = Path.Combine(AppContext.BaseDirectory, fileName);
        if (File.Exists(localAppPath))
        {
            return localAppPath;
        }

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            foreach (var candidateDirectory in CandidateDirectories)
            {
                var candidatePath = Path.Combine(current.FullName, candidateDirectory, fileName);
                if (File.Exists(candidatePath))
                {
                    return candidatePath;
                }
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Unable to locate {fileName}. Expected one of the known tool bundle layouts.");
    }
}
