using BlockchainHistory.Application.Abstractions;
using BlockchainHistory.Application.Snapshots.Capture;
using BlockchainHistory.Domain.Chains;
using BlockchainHistory.Domain.Snapshots;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace BlockchainHistory.UnitTests.Application;

public sealed class CaptureSnapshotHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    private readonly IBlockCypherClient _client = Substitute.For<IBlockCypherClient>();
    private readonly ISnapshotRepository _repository = Substitute.For<ISnapshotRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CaptureSnapshotHandler _handler;

    public CaptureSnapshotHandlerTests()
    {
        _handler = new CaptureSnapshotHandler(_client, _repository, _unitOfWork, new FakeTimeProvider(Now));
    }

    [Fact]
    public async Task Handle_FetchesAddsSavesAndReturnsDto()
    {
        const string raw = """{"name":"BTC.main"}""";
        _client.GetChainAsync(BlockchainNetwork.BtcMain, Arg.Any<CancellationToken>())
            .Returns(new ChainFetchResult(TestChainStatus.Utxo(), raw));

        var dto = await _handler.Handle(new CaptureSnapshotCommand(BlockchainNetwork.BtcMain), TestContext.Current.CancellationToken);

        _repository.Received(1).Add(Arg.Is<BlockchainSnapshot>(s =>
            s.Network == BlockchainNetwork.BtcMain && s.RawJson == raw && s.CreatedAt == Now));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Equal("btc-main", dto.Network);
        Assert.Equal(Now, dto.CreatedAt);
    }

    [Fact]
    public async Task Handle_UpstreamUnavailable_PropagatesAndSavesNothing()
    {
        _client.GetChainAsync(BlockchainNetwork.BtcMain, Arg.Any<CancellationToken>())
            .ThrowsAsync(new BlockCypherUnavailableException(BlockchainNetwork.BtcMain, "down"));

        await Assert.ThrowsAsync<BlockCypherUnavailableException>(() =>
            _handler.Handle(new CaptureSnapshotCommand(BlockchainNetwork.BtcMain), TestContext.Current.CancellationToken).AsTask());

        _repository.DidNotReceiveWithAnyArgs().Add(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
