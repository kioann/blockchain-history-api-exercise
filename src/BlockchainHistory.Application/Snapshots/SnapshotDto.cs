using System.Text.Json;

namespace BlockchainHistory.Application.Snapshots;

/// <summary>A captured snapshot as returned by the API.</summary>
/// <param name="Network">The network slug, e.g. <c>btc-main</c>.</param>
/// <param name="UtxoFees">Fees per kB for Bitcoin-style chains; null for Ethereum.</param>
/// <param name="EthereumFees">Gas prices and fees in wei for Ethereum; null for Bitcoin-style chains.</param>
/// <param name="CreatedAt">When this service captured the snapshot (UTC).</param>
/// <param name="Raw">The provider's response exactly as received.</param>
public sealed record SnapshotDto(
    Guid Id,
    string Network,
    string Name,
    long Height,
    string Hash,
    DateTimeOffset Time,
    string LatestUrl,
    string PreviousHash,
    string PreviousUrl,
    long PeerCount,
    long UnconfirmedCount,
    long? LastForkHeight,
    string? LastForkHash,
    UtxoFeesDto? UtxoFees,
    EthereumFeesDto? EthereumFees,
    DateTimeOffset CreatedAt,
    JsonElement Raw);

public sealed record UtxoFeesDto(long? HighFeePerKb, long? MediumFeePerKb, long? LowFeePerKb);

public sealed record EthereumFeesDto(
    long? HighGasPrice,
    long? MediumGasPrice,
    long? LowGasPrice,
    long? HighPriorityFee,
    long? MediumPriorityFee,
    long? LowPriorityFee,
    long? BaseFee);
