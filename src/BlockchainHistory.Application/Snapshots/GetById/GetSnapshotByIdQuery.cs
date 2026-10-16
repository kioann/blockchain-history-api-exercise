using BlockchainHistory.Application.Abstractions;
using Mediator;

namespace BlockchainHistory.Application.Snapshots.GetById;

/// <summary>A single stored snapshot, or null when the id is unknown.</summary>
public sealed record GetSnapshotByIdQuery(Guid Id) : IQuery<SnapshotDto?>;

public sealed class GetSnapshotByIdHandler(ISnapshotRepository repository)
    : IQueryHandler<GetSnapshotByIdQuery, SnapshotDto?>
{
    public async ValueTask<SnapshotDto?> Handle(GetSnapshotByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var snapshot = await repository.GetByIdAsync(query.Id, cancellationToken);
        return snapshot is null ? null : SnapshotMapper.ToDto(snapshot);
    }
}
