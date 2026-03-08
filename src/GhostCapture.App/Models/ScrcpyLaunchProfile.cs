namespace GhostCapture.App.Models;

public sealed class ScrcpyLaunchProfile
{
    public required string Name { get; init; }

    public required IReadOnlyList<string> Arguments { get; init; }

    public static ScrcpyLaunchProfile CompetitiveLowLatency()
    {
        return new ScrcpyLaunchProfile
        {
            Name = "Competitive Low Latency",
            Arguments = new[]
            {
                "--video-codec=h264",
                "--video-buffer=0",
                "--audio-buffer=20",
                "--audio-output-buffer=5",
                "--video-bit-rate=16M",
                "--max-size=1920",
                "--max-fps=120",
                "--window-title=GhostCapture",
            },
        };
    }
}

