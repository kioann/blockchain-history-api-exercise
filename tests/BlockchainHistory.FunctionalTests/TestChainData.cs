using System.Globalization;
using BlockchainHistory.Application.Abstractions;
using BlockchainHistory.Domain.Chains;
using BlockchainHistory.Domain.Snapshots;

namespace BlockchainHistory.FunctionalTests;

/// <summary>Fake BlockCypher answers for each chain.</summary>
internal static class TestChainData
{
    public static ChainFetchResult For(BlockchainNetwork network)
    {
        var info = BlockchainNetworks.Get(network);
        var name = $"{info.Coin.ToUpperInvariant()}.{info.Chain}";
        var height = 1000 + (int)network;
        var isEthereum = info.Model == ChainModel.Account;

        var status = new ChainStatus
        {
            Name = name,
            Height = height,
            Hash = $"hash-{info.Slug}",
            Time = new DateTimeOffset(2026, 10, 16, 11, 59, 0, TimeSpan.Zero),
            LatestUrl = $"https://api.blockcypher.com/v1/{info.Coin}/{info.Chain}/blocks/hash-{info.Slug}",
            PreviousHash = "previous",
            PreviousUrl = $"https://api.blockcypher.com/v1/{info.Coin}/{info.Chain}/blocks/previous",
            PeerCount = 10,
            UnconfirmedCount = 5,
            HighFeePerKb = isEthereum ? null : 3000,
            MediumFeePerKb = isEthereum ? null : 2000,
            LowFeePerKb = isEthereum ? null : 1000,
            HighGasPrice = isEthereum ? 30 : null,
            BaseFee = isEthereum ? 10 : null,
        };

        var raw = string.Create(CultureInfo.InvariantCulture, $$"""{"name":"{{name}}","height":{{height}}}""");
        return new ChainFetchResult(status, raw);
    }
}
