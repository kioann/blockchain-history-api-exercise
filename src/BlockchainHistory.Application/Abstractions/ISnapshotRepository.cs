using BlockchainHistory.Application.Common;
using BlockchainHistory.Domain.Chains;
using BlockchainHistory.Domain.Snapshots;

namespace BlockchainHistory.Application.Abstractions;

/// <summary>Stores and reads snapshots. Save with <see cref="IUnitOfWork"/>.</summary>
public interface ISnapshotRepository
{
    void Add(BlockchainSnapshot snapshot);

    /// <summary>One page of a chain's history, newest first.</summary>
    Task<PagedResult<BlockchainSnapshot>> GetHistoryAsync(
        BlockchainNetwork network, int page, int pageSize, CancellationToken cancellationToken);

    Task<BlockchainSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
