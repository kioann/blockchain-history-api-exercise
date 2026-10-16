using System.Text.Json.Serialization;
using BlockchainHistory.Domain.Snapshots;

namespace BlockchainHistory.Infrastructure.BlockCypher;

/// <summary>
/// BlockCypher's <c>GET /v1/{coin}/{chain}</c> response. Required members fail deserialisation when missing;
/// fee and fork fields are optional because they differ per chain.
/// </summary>
internal sealed record BlockCypherChainResponse
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

    public ChainStatus ToChainStatus() => new()
    {
        Name = Name,
        Height = Height,
        Hash = Hash,
        Time = Time,
        LatestUrl = LatestUrl,
        PreviousHash = PreviousHash,
        PreviousUrl = PreviousUrl,
        PeerCount = PeerCount,
        UnconfirmedCount = UnconfirmedCount,
        LastForkHeight = LastForkHeight,
        LastForkHash = LastForkHash,
        HighFeePerKb = HighFeePerKb,
        MediumFeePerKb = MediumFeePerKb,
        LowFeePerKb = LowFeePerKb,
        HighGasPrice = HighGasPrice,
        MediumGasPrice = MediumGasPrice,
        LowGasPrice = LowGasPrice,
        HighPriorityFee = HighPriorityFee,
        MediumPriorityFee = MediumPriorityFee,
        LowPriorityFee = LowPriorityFee,
        BaseFee = BaseFee,
    };
}

/// <summary>Compile-time JSON serialisation metadata: no reflection, fewer allocations.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(BlockCypherChainResponse))]
internal sealed partial class BlockCypherJsonContext : JsonSerializerContext;
