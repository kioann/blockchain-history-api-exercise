using BlockchainHistory.Application.Abstractions;
using BlockchainHistory.Application.Common;
using BlockchainHistory.Domain.Chains;
using BlockchainHistory.Domain.Snapshots;
using Microsoft.EntityFrameworkCore;

namespace BlockchainHistory.Infrastructure.Persistence;

internal sealed class SnapshotRepository(BlockchainHistoryDbContext db) : ISnapshotRepository
{
    public void Add(BlockchainSnapshot snapshot) => db.Snapshots.Add(snapshot);

    public async Task<PagedResult<BlockchainSnapshot>> GetHistoryAsync(
        BlockchainNetwork network, int page, int pageSize, CancellationToken cancellationToken)
    {
        // Read-only, so skip change tracking.
        var query = db.Snapshots.AsNoTracking().Where(s => s.Network == network);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .ThenByDescending(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<BlockchainSnapshot>(items, page, pageSize, total);
    }

    public Task<BlockchainSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Snapshots.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
}
