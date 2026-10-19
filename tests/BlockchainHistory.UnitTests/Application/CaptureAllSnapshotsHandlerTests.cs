using BlockchainHistory.Application.Abstractions;
using BlockchainHistory.Application.Snapshots.CaptureAll;
using BlockchainHistory.Domain.Chains;
using BlockchainHistory.Domain.Snapshots;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace BlockchainHistory.UnitTests.Application;

public sealed class CaptureAllSnapshotsHandlerTests
{
    private readonly IBlockCypherClient _client = Substitute.For<IBlockCypherClient>();
    private readonly ISnapshotRepository _repository = Substitute.For<ISnapshotRepository>();
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _clock = new(Now);
    private readonly CaptureAllSnapshotsHandler _handler;

    public CaptureAllSnapshotsHandlerTests()
    {
        _handler = new CaptureAllSnapshotsHandler(
            _client, _repository, _unitOfWork, _clock, NullLogger<CaptureAllSnapshotsHandler>.Instance);

        _client.GetChainAsync(Arg.Any<BlockchainNetwork>(), Arg.Any<CancellationToken>())
            .Returns(call => new ChainFetchResult(
                call.Arg<BlockchainNetwork>() == BlockchainNetwork.EthMain ? TestChainStatus.Ethereum() : TestChainStatus.Utxo(),
                "{}"));
    }

    [Fact]
    public async Task Handle_StampsCreatedAtWhenTheRequestArrives_NotAfterSlowFetches()
    {
        _client.GetChainAsync(BlockchainNetwork.LtcMain, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _clock.Advance(TimeSpan.FromSeconds(30));
            return new ChainFetchResult(TestChainStatus.Utxo(), "{}");
        });

        var result = await _handler.Handle(new CaptureAllSnapshotsCommand(), TestContext.Current.CancellationToken);

        Assert.All(result.Captured, s => Assert.Equal(Now, s.CreatedAt));
    }

    [Fact]
    public async Task Handle_CallsEachOfTheFiveNetworksOnce()
    {
        var result = await _handler.Handle(new CaptureAllSnapshotsCommand(), TestContext.Current.CancellationToken);

        foreach (var network in Enum.GetValues<BlockchainNetwork>())
        {
            await _client.Received(1).GetChainAsync(network, Arg.Any<CancellationToken>());
        }

        Assert.Equal(5, result.Captured.Count);
        Assert.Empty(result.Failed);
        _repository.Received(5).Add(Arg.Any<BlockchainSnapshot>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OneNetworkFails_OthersSavedInOneBatchAndFailureReported()
    {
        _client.GetChainAsync(BlockchainNetwork.DashMain, Arg.Any<CancellationToken>())
            .ThrowsAsync(new BlockCypherUnavailableException(BlockchainNetwork.DashMain, "429 Too Many Requests"));

        var result = await _handler.Handle(new CaptureAllSnapshotsCommand(), TestContext.Current.CancellationToken);

        Assert.Equal(4, result.Captured.Count);
        var failure = Assert.Single(result.Failed);
        Assert.Equal("dash-main", failure.Network);
        Assert.Contains("429", failure.Error, StringComparison.Ordinal);
        _repository.Received(4).Add(Arg.Any<BlockchainSnapshot>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AllNetworksFail_SavesNothing()
    {
        _client.GetChainAsync(Arg.Any<BlockchainNetwork>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(call => new BlockCypherUnavailableException(call.Arg<BlockchainNetwork>(), "down"));

        var result = await _handler.Handle(new CaptureAllSnapshotsCommand(), TestContext.Current.CancellationToken);

        Assert.Empty(result.Captured);
        Assert.Equal(5, result.Failed.Count);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
