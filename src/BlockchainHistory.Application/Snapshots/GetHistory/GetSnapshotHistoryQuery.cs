using BlockchainHistory.Application.Abstractions;
using BlockchainHistory.Application.Common;
using BlockchainHistory.Domain.Chains;
using FluentValidation;
using Mediator;

namespace BlockchainHistory.Application.Snapshots.GetHistory;

/// <summary>One page of a chain's history, newest first.</summary>
public sealed record GetSnapshotHistoryQuery(BlockchainNetwork Network, int Page, int PageSize)
    : IQuery<PagedResult<SnapshotDto>>;

public sealed class GetSnapshotHistoryQueryValidator : AbstractValidator<GetSnapshotHistoryQuery>
{
    public const int MaxPageSize = 100;

    public GetSnapshotHistoryQueryValidator()
    {
        RuleFor(q => q.Network).IsInEnum();
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, MaxPageSize);
    }
}

public sealed class GetSnapshotHistoryHandler(ISnapshotRepository repository)
    : IQueryHandler<GetSnapshotHistoryQuery, PagedResult<SnapshotDto>>
{
    public async ValueTask<PagedResult<SnapshotDto>> Handle(
        GetSnapshotHistoryQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var page = await repository.GetHistoryAsync(query.Network, query.Page, query.PageSize, cancellationToken);
        return new PagedResult<SnapshotDto>(
            page.Items.Select(SnapshotMapper.ToDto).ToList(), page.Page, page.PageSize, page.TotalCount);
    }
}
