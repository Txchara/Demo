namespace Tool.Core.Network;

public sealed class PingResult
{
    public string Address { get; init; } = string.Empty;

    public bool Success { get; init; }

    public long RoundtripTime { get; init; }

    public string Status { get; init; } = string.Empty;

    public string? IpAddress { get; init; }

    public string? ErrorMessage { get; init; }
}
