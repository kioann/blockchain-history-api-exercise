using BlockchainHistory.Domain.Chains;
using BlockchainHistory.Domain.Snapshots;
using BlockchainHistory.Infrastructure.Persistence;
using Mediator;
using NSubstitute;

namespace BlockchainHistory.IntegrationTests.Persistence;

public sealed class SnapshotRepositoryTests : IAsyncLifetime
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    private SqliteDatabase _database = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => _database = await SqliteDatabase.CreateAsync(Ct);

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    [Fact]
    public async Task GetHistory_OrdersByCreatedAtDescending()
    {
        var first = await SaveAsync(BlockchainNetwork.BtcMain, T0);
        var third = await SaveAsync(BlockchainNetwork.BtcMain, T0.AddMinutes(2));
        var second = await SaveAsync(BlockchainNetwork.BtcMain, T0.AddMinutes(1));

        var page = await NewRepository().GetHistoryAsync(BlockchainNetwork.BtcMain, 1, 10, Ct);

        Assert.Equal([third.Id, second.Id, first.Id], page.Items.Select(s => s.Id));
    }

    [Fact]
    public async Task GetHistory_FiltersByNetwork()
    {
        var btc = await SaveAsync(BlockchainNetwork.BtcMain, T0);
        await SaveAsync(BlockchainNetwork.LtcMain, T0);

        var page = await NewRepository().GetHistoryAsync(BlockchainNetwork.BtcMain, 1, 10, Ct);

        Assert.Equal(btc.Id, Assert.Single(page.Items).Id);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task GetHistory_PagesAndCounts()
    {
        for (var i = 0; i < 5; i++)
        {
            await SaveAsync(BlockchainNetwork.EthMain, T0.AddMinutes(i));
        }

        var page = await NewRepository().GetHistoryAsync(BlockchainNetwork.EthMain, 2, 2, Ct);

        Assert.Equal([T0.AddMinutes(2), T0.AddMinutes(1)], page.Items.Select(s => s.CreatedAt));
        Assert.Equal((2, 2, 5, 3), (page.Page, page.PageSize, page.TotalCount, page.TotalPages));
    }

    [Fact]
    public async Task GetHistory_PageBeyondEnd_ReturnsEmptyWithTotal()
    {
        await SaveAsync(BlockchainNetwork.EthMain, T0);

        var page = await NewRepository().GetHistoryAsync(BlockchainNetwork.EthMain, 5, 20, Ct);

        Assert.Empty(page.Items);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task RoundTrip_PreservesSubtypeFeesAndRawJsonExactly()
    {
        var ethRaw = await File.ReadAllTextAsync(Path.Combine("Fixtures", "eth-main.json"), Ct);
        var btcRaw = await File.ReadAllTextAsync(Path.Combine("Fixtures", "btc-main.json"), Ct);
        var eth = await SaveAsync(BlockchainNetwork.EthMain, T0, ethRaw);
        var btc = await SaveAsync(BlockchainNetwork.BtcMain, T0, btcRaw);

        var repository = NewRepository();
        var loadedEth = Assert.IsType<EthereumSnapshot>(await repository.GetByIdAsync(eth.Id, Ct));
        var loadedBtc = Assert.IsType<UtxoChainSnapshot>(await repository.GetByIdAsync(btc.Id, Ct));

        Assert.Equal(ethRaw, loadedEth.RawJson);
        Assert.Equal(btcRaw, loadedBtc.RawJson);
        Assert.Equal(TestData.Ethereum().BaseFee, loadedEth.BaseFee);
        Assert.Equal(TestData.Utxo().HighFeePerKb, loadedBtc.HighFeePerKb);
        Assert.Equal(T0, loadedEth.CreatedAt);
        Assert.Equal(TestData.Ethereum().Time, loadedEth.Time);
    }

    [Fact]
    public async Task GetById_Unknown_ReturnsNull()
    {
        Assert.Null(await NewRepository().GetByIdAsync(Guid.NewGuid(), Ct));
    }

    [Fact]
    public async Task SaveChanges_PublishesSnapshotCapturedAfterSaving()
    {
        var snapshot = await SaveAsync(BlockchainNetwork.DashMain, T0);

        await _database.Publisher.Received(1).Publish(
            Arg.Is<object>(e => e.Equals(new SnapshotCaptured(snapshot.Id, BlockchainNetwork.DashMain, snapshot.Height))),
            Arg.Any<CancellationToken>());
        Assert.Empty(snapshot.DomainEvents);
    }

    private SnapshotRepository NewRepository() => new(_database.NewContext());

    private async Task<BlockchainSnapshot> SaveAsync(BlockchainNetwork network, DateTimeOffset createdAt, string raw = "{}")
    {
        var status = BlockchainNetworks.Get(network).Model == ChainModel.Account ? TestData.Ethereum() : TestData.Utxo();
        var snapshot = BlockchainSnapshot.Capture(network, status, raw, createdAt);

        await using var context = _database.NewContext();
        new SnapshotRepository(context).Add(snapshot);
        await context.SaveChangesAsync(Ct);
        return snapshot;
    }
}
