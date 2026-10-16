using BlockchainHistory.Application.Common;
using BlockchainHistory.Domain.Chains;
using BlockchainHistory.Domain.Snapshots;

namespace BlockchainHistory.Application.Abstractions;

/// <summary>Persistence for snapshots. Changes are committed through <see cref="IUnitOfWork"/>.</summary>
public interface ISnapshotRepository
{
    void Add(BlockchainSnapshot snapshot);

    /// <summary>One page of a network's history, newest <see cref="BlockchainSnapshot.CreatedAt"/> first.</summary>
    Task<PagedResult<BlockchainSnapshot>> GetHistoryAsync(
        BlockchainNetwork network, int page, int pageSize, CancellationToken cancellationToken);

    Task<BlockchainSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
