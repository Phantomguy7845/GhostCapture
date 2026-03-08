using System.IO;
using System.Text;

namespace GhostCapture.App.Services;

public static class DiagnosticLog
{
    private static readonly object SyncRoot = new();
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GhostCapture",
        "logs");

    public static void Write(string category, string message)
    {
        var line = $"{DateTime.Now:O} [{category}] {message}{Environment.NewLine}";

        lock (SyncRoot)
        {
            Directory.CreateDirectory(LogDirectory);
            var path = Path.Combine(LogDirectory, $"ghostcapture-trace-{DateTime.Now:yyyyMMdd}.log");
            File.AppendAllText(path, line, Encoding.UTF8);
        }
    }
}
