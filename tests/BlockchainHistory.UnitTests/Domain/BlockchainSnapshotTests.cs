using BlockchainHistory.Domain.Chains;
using BlockchainHistory.Domain.Snapshots;

namespace BlockchainHistory.UnitTests.Domain;

public sealed class BlockchainSnapshotTests
{
    private static readonly DateTimeOffset CapturedAt = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Capture_UtxoNetwork_CreatesUtxoSnapshotWithFees()
    {
        var status = TestChainStatus.Utxo() with { HighFeePerKb = 2915, MediumFeePerKb = 1756, LowFeePerKb = 1396 };

        var snapshot = BlockchainSnapshot.Capture(BlockchainNetwork.BtcMain, status, "{}", CapturedAt);

        var utxo = Assert.IsType<UtxoChainSnapshot>(snapshot);
        Assert.Equal((2915L, 1756L, 1396L), (utxo.HighFeePerKb, utxo.MediumFeePerKb, utxo.LowFeePerKb));
    }

    [Fact]
    public void Capture_EthNetwork_CreatesEthereumSnapshotWithGas()
    {
        var status = TestChainStatus.Ethereum() with { HighGasPrice = 3439662035, BaseFee = 155630516 };

        var snapshot = BlockchainSnapshot.Capture(BlockchainNetwork.EthMain, status, "{}", CapturedAt);

        var eth = Assert.IsType<EthereumSnapshot>(snapshot);
        Assert.Equal(3439662035L, eth.HighGasPrice);
        Assert.Equal(155630516L, eth.BaseFee);
    }

    [Fact]
    public void Capture_CopiesCommonFields()
    {
        var status = TestChainStatus.Utxo();

        var snapshot = BlockchainSnapshot.Capture(BlockchainNetwork.BtcMain, status, "{}", CapturedAt);

        Assert.Equal(BlockchainNetwork.BtcMain, snapshot.Network);
        Assert.Equal(status.Name, snapshot.Name);
        Assert.Equal(status.Height, snapshot.Height);
        Assert.Equal(status.Hash, snapshot.Hash);
        Assert.Equal(status.PeerCount, snapshot.PeerCount);
        Assert.Equal(status.UnconfirmedCount, snapshot.UnconfirmedCount);
        Assert.Equal(status.LastForkHeight, snapshot.LastForkHeight);
    }

    [Fact]
    public void Capture_StoresRawJsonAndCreatedAtVerbatim()
    {
        const string raw = "{ \"name\": \"BTC.main\",  \"height\": 971714 }";

        var snapshot = BlockchainSnapshot.Capture(BlockchainNetwork.BtcMain, TestChainStatus.Utxo(), raw, CapturedAt);

        Assert.Equal(raw, snapshot.RawJson);
        Assert.Equal(CapturedAt, snapshot.CreatedAt);
    }

    [Fact]
    public void Capture_AssignsTimeOrderedId()
    {
        var snapshot = BlockchainSnapshot.Capture(BlockchainNetwork.BtcMain, TestChainStatus.Utxo(), "{}", CapturedAt);

        Assert.NotEqual(Guid.Empty, snapshot.Id);
        Assert.Equal(7, snapshot.Id.Version);
    }

    [Fact]
    public void Capture_RaisesSnapshotCapturedEvent()
    {
        var snapshot = BlockchainSnapshot.Capture(BlockchainNetwork.LtcMain, TestChainStatus.Utxo(), "{}", CapturedAt);

        var raised = Assert.Single(snapshot.DomainEvents);
        Assert.Equal(new SnapshotCaptured(snapshot.Id, BlockchainNetwork.LtcMain, snapshot.Height), raised);
    }

    [Fact]
    public void ClearDomainEvents_EmptiesTheList()
    {
        var snapshot = BlockchainSnapshot.Capture(BlockchainNetwork.LtcMain, TestChainStatus.Utxo(), "{}", CapturedAt);

        snapshot.ClearDomainEvents();

        Assert.Empty(snapshot.DomainEvents);
    }
}
