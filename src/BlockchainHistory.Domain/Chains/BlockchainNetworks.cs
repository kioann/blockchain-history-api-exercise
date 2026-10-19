using System.Diagnostics.CodeAnalysis;

namespace BlockchainHistory.Domain.Chains;

/// <summary>Registry of supported networks and lookups by enum or slug.</summary>
public static class BlockchainNetworks
{
    public static IReadOnlyList<NetworkInfo> All { get; } =
    [
        new(BlockchainNetwork.EthMain, "eth", "main", ChainModel.Account),
        new(BlockchainNetwork.DashMain, "dash", "main", ChainModel.Utxo),
        new(BlockchainNetwork.BtcMain, "btc", "main", ChainModel.Utxo),
        new(BlockchainNetwork.BtcTest3, "btc", "test3", ChainModel.Utxo),
        new(BlockchainNetwork.LtcMain, "ltc", "main", ChainModel.Utxo),
    ];

    /// <summary>Comma-separated list of every supported slug, for error messages.</summary>
    public static string SupportedSlugs { get; } = string.Join(", ", All.Select(n => n.Slug));

    private static readonly Dictionary<BlockchainNetwork, NetworkInfo> ByNetwork = All.ToDictionary(n => n.Network);

    private static readonly Dictionary<string, NetworkInfo> BySlug =
        All.ToDictionary(n => n.Slug, StringComparer.OrdinalIgnoreCase);

    public static NetworkInfo Get(BlockchainNetwork network) => ByNetwork[network];

    public static string ToSlug(this BlockchainNetwork network) => Get(network).Slug;

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
