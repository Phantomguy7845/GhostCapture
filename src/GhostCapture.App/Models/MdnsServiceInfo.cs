namespace GhostCapture.App.Models;

public sealed class MdnsServiceInfo
{
    public required string InstanceName { get; init; }

    public required string ServiceType { get; init; }

    public required string Endpoint { get; init; }
}

