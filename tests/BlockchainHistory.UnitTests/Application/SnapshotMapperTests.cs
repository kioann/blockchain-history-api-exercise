using System.Text.Json;
using BlockchainHistory.Application.Snapshots;
using BlockchainHistory.Domain.Chains;
using BlockchainHistory.Domain.Snapshots;

namespace BlockchainHistory.UnitTests.Application;

public sealed class SnapshotMapperTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ToDto_UtxoSnapshot_HasUtxoFeesOnly()
    {
        var snapshot = BlockchainSnapshot.Capture(BlockchainNetwork.BtcMain, TestChainStatus.Utxo(), "{}", Now);

        var dto = SnapshotMapper.ToDto(snapshot);

        Assert.Equal(new UtxoFeesDto(2915, 1756, 1396), dto.UtxoFees);
        Assert.Null(dto.EthereumFees);
    }

    [Fact]
    public void ToDto_EthereumSnapshot_HasEthereumFeesOnly()
    {
        var snapshot = BlockchainSnapshot.Capture(BlockchainNetwork.EthMain, TestChainStatus.Ethereum(), "{}", Now);

        var dto = SnapshotMapper.ToDto(snapshot);

        Assert.Equal(
            new EthereumFeesDto(3439662035, 2492686971, 1376987995, 1752375086, 445410673, 210781767, 155630516),
            dto.EthereumFees);
        Assert.Null(dto.UtxoFees);
    }

    [Fact]
    public void ToDto_CopiesCommonFieldsAndUsesSlug()
    {
        var snapshot = BlockchainSnapshot.Capture(BlockchainNetwork.BtcTest3, TestChainStatus.Utxo(), "{}", Now);

        var dto = SnapshotMapper.ToDto(snapshot);

        Assert.Equal((snapshot.Id, "btc-test3", snapshot.Height, snapshot.Hash, Now),
            (dto.Id, dto.Network, dto.Height, dto.Hash, dto.CreatedAt));
    }

    [Fact]
    public void ToDto_RawIsTheParsedProviderJson()
    {
        var snapshot = BlockchainSnapshot.Capture(
            BlockchainNetwork.BtcMain, TestChainStatus.Utxo(), """{"name":"BTC.main","height":971714}""", Now);

        var dto = SnapshotMapper.ToDto(snapshot);

        Assert.Equal(JsonValueKind.Object, dto.Raw.ValueKind);
        Assert.Equal(971714, dto.Raw.GetProperty("height").GetInt64());
    }
}
