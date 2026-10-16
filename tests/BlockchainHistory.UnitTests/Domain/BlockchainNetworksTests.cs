using BlockchainHistory.Domain.Chains;

namespace BlockchainHistory.UnitTests.Domain;

public sealed class BlockchainNetworksTests
{
    [Fact]
    public void All_ContainsTheFiveSupportedNetworks()
    {
        var slugs = BlockchainNetworks.All.Select(n => n.Slug);

        Assert.Equal(["eth-main", "dash-main", "btc-main", "btc-test3", "ltc-main"], slugs);
    }

    [Theory]
    [InlineData("btc-main", BlockchainNetwork.BtcMain)]
    [InlineData("BTC-Main", BlockchainNetwork.BtcMain)]
    [InlineData("eth-main", BlockchainNetwork.EthMain)]
    [InlineData("btc-test3", BlockchainNetwork.BtcTest3)]
    public void TryParse_KnownSlug_IsCaseInsensitive(string slug, BlockchainNetwork expected)
    {
        var parsed = BlockchainNetworks.TryParse(slug, out var network);

        Assert.True(parsed);
        Assert.Equal(expected, network);
    }

    [Theory]
    [InlineData("doge-main")]
    [InlineData("btc_main")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParse_UnknownSlug_ReturnsFalse(string? slug)
    {
        Assert.False(BlockchainNetworks.TryParse(slug, out _));
    }

    [Fact]
    public void Get_ReturnsBlockCypherPathAndChainModel()
    {
        var eth = BlockchainNetworks.Get(BlockchainNetwork.EthMain);
        var ltc = BlockchainNetworks.Get(BlockchainNetwork.LtcMain);

        Assert.Equal(("eth", "main", ChainModel.Account), (eth.Coin, eth.Chain, eth.Model));
        Assert.Equal(("ltc", "main", ChainModel.Utxo), (ltc.Coin, ltc.Chain, ltc.Model));
    }
}
