using System.Text.Json;
using BlockchainHistory.Domain.Chains;
using BlockchainHistory.Domain.Snapshots;
using Riok.Mapperly.Abstractions;

namespace BlockchainHistory.Application.Snapshots;

/// <summary>Entity to DTO mapping, generated at compile time by Mapperly.</summary>
[Mapper]
public static partial class SnapshotMapper
{
    [MapperIgnoreSource(nameof(BlockchainSnapshot.DomainEvents))]
    [MapProperty(nameof(BlockchainSnapshot.RawJson), nameof(SnapshotDto.Raw), Use = nameof(ParseRaw))]
    [MapPropertyFromSource(nameof(SnapshotDto.UtxoFees), Use = nameof(ToUtxoFees))]
    [MapPropertyFromSource(nameof(SnapshotDto.EthereumFees), Use = nameof(ToEthereumFees))]
    public static partial SnapshotDto ToDto(BlockchainSnapshot snapshot);

    private static string ToSlug(BlockchainNetwork network) => BlockchainNetworks.Get(network).Slug;

    private static JsonElement ParseRaw(string rawJson) => JsonSerializer.Deserialize<JsonElement>(rawJson);

    private static UtxoFeesDto? ToUtxoFees(BlockchainSnapshot snapshot) => snapshot is UtxoChainSnapshot u
        ? new UtxoFeesDto(u.HighFeePerKb, u.MediumFeePerKb, u.LowFeePerKb)
        : null;

    private static EthereumFeesDto? ToEthereumFees(BlockchainSnapshot snapshot) => snapshot is EthereumSnapshot e
        ? new EthereumFeesDto(
            e.HighGasPrice, e.MediumGasPrice, e.LowGasPrice,
            e.HighPriorityFee, e.MediumPriorityFee, e.LowPriorityFee, e.BaseFee)
        : null;
}
