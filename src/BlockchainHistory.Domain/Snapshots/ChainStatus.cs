namespace BlockchainHistory.Domain.Snapshots;

/// <summary>Chain data from the provider, before it becomes a snapshot. Each chain fills in only its own fee fields.</summary>
public sealed record ChainStatus
{
    public required string Name { get; init; }
    public required long Height { get; init; }
    public required string Hash { get; init; }
    public required DateTimeOffset Time { get; init; }
    public required string LatestUrl { get; init; }
    public required string PreviousHash { get; init; }
    public required string PreviousUrl { get; init; }
    public required long PeerCount { get; init; }
    public required long UnconfirmedCount { get; init; }
    public long? LastForkHeight { get; init; }
    public string? LastForkHash { get; init; }

    public long? HighFeePerKb { get; init; }
    public long? MediumFeePerKb { get; init; }
    public long? LowFeePerKb { get; init; }

    public long? HighGasPrice { get; init; }
    public long? MediumGasPrice { get; init; }
    public long? LowGasPrice { get; init; }
    public long? HighPriorityFee { get; init; }
    public long? MediumPriorityFee { get; init; }
    public long? LowPriorityFee { get; init; }
    public long? BaseFee { get; init; }
}
