using System.Text.Json;

namespace BlockchainHistory.Application.Snapshots;

/// <summary>A snapshot as the API returns it.</summary>
/// <param name="Network">Chain slug, e.g. btc-main.</param>
/// <param name="UtxoFees">Fees per kB (Bitcoin-style chains only).</param>
/// <param name="EthereumFees">Gas prices in wei (Ethereum only).</param>
/// <param name="CreatedAt">When we captured it (UTC).</param>
/// <param name="Raw">BlockCypher's original response.</param>
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
