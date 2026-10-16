using System.Diagnostics.CodeAnalysis;

namespace BlockchainHistory.Domain.Chains;

/// <summary>Registry of supported networks and lookups by enum or slug.</summary>
public static class BlockchainNetworks
{
    public static IReadOnlyList<NetworkInfo> All { get; } =
    [
        new(BlockchainNetwork.EthMain, "eth-main", "eth", "main", ChainModel.Account),
        new(BlockchainNetwork.DashMain, "dash-main", "dash", "main", ChainModel.Utxo),
        new(BlockchainNetwork.BtcMain, "btc-main", "btc", "main", ChainModel.Utxo),
        new(BlockchainNetwork.BtcTest3, "btc-test3", "btc", "test3", ChainModel.Utxo),
        new(BlockchainNetwork.LtcMain, "ltc-main", "ltc", "main", ChainModel.Utxo),
    ];

    private static readonly Dictionary<BlockchainNetwork, NetworkInfo> ByNetwork = All.ToDictionary(n => n.Network);

    private static readonly Dictionary<string, NetworkInfo> BySlug =
        All.ToDictionary(n => n.Slug, StringComparer.OrdinalIgnoreCase);

    public static NetworkInfo Get(BlockchainNetwork network) => ByNetwork[network];

    public static bool TryParse([NotNullWhen(true)] string? slug, out BlockchainNetwork network)
    {
        if (slug is not null && BySlug.TryGetValue(slug, out var info))
        {
            network = info.Network;
            return true;
        }

        network = default;
        return false;
    }
}
