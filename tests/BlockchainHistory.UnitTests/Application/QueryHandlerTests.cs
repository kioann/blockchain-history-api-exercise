using BlockchainHistory.Application.Abstractions;
using BlockchainHistory.Application.Common;
using BlockchainHistory.Application.Snapshots.GetById;
using BlockchainHistory.Application.Snapshots.GetHistory;
using BlockchainHistory.Domain.Chains;
using BlockchainHistory.Domain.Snapshots;
using NSubstitute;

namespace BlockchainHistory.UnitTests.Application;

public sealed class QueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    private readonly ISnapshotRepository _repository = Substitute.For<ISnapshotRepository>();

    [Fact]
    public async Task GetHistory_MapsPagedResult()
    {
        var newer = BlockchainSnapshot.Capture(BlockchainNetwork.BtcMain, TestChainStatus.Utxo(), "{}", Now.AddMinutes(1));
        var older = BlockchainSnapshot.Capture(BlockchainNetwork.BtcMain, TestChainStatus.Utxo(), "{}", Now);
        _repository.GetHistoryAsync(BlockchainNetwork.BtcMain, 1, 2, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BlockchainSnapshot>([newer, older], 1, 2, 7));

        var result = await new GetSnapshotHistoryHandler(_repository)
            .Handle(new GetSnapshotHistoryQuery(BlockchainNetwork.BtcMain, 1, 2), TestContext.Current.CancellationToken);

        Assert.Equal([newer.Id, older.Id], result.Items.Select(i => i.Id));
        Assert.Equal((1, 2, 7, 4), (result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    [Fact]
    public async Task GetHistory_PageBeyondEnd_ReturnsEmptyItemsWithTotal()
    {
        _repository.GetHistoryAsync(BlockchainNetwork.BtcMain, 9, 20, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BlockchainSnapshot>([], 9, 20, 3));

        var result = await new GetSnapshotHistoryHandler(_repository)
            .Handle(new GetSnapshotHistoryQuery(BlockchainNetwork.BtcMain, 9, 20), TestContext.Current.CancellationToken);

        Assert.Empty(result.Items);
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task GetById_Unknown_ReturnsNull()
    {
        var result = await new GetSnapshotByIdHandler(_repository)
            .Handle(new GetSnapshotByIdQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.Null(result);
    }
}
