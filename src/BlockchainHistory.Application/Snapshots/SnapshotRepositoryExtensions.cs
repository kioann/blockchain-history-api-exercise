using BlockchainHistory.Application.Abstractions;
using BlockchainHistory.Domain.Chains;
using BlockchainHistory.Domain.Snapshots;

namespace BlockchainHistory.Application.Snapshots;

internal static class SnapshotRepositoryExtensions
{
    /// <summary>Builds a snapshot, adds it for saving, and returns its DTO.</summary>
    public static SnapshotDto AddCaptured(
        this ISnapshotRepository repository, BlockchainNetwork network, ChainFetchResult fetched, DateTimeOffset createdAt)
    {
        var snapshot = BlockchainSnapshot.Capture(network, fetched.Status, fetched.RawJson, createdAt);
        repository.Add(snapshot);
        return SnapshotMapper.ToDto(snapshot);
    }
}
