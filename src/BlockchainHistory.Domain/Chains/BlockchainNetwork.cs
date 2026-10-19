namespace BlockchainHistory.Domain.Chains;

public enum BlockchainNetwork
{
    EthMain,
    DashMain,
    BtcMain,
    BtcTest3,
    LtcMain,
}

/// <summary>Decides how a chain reports its fees.</summary>
public enum ChainModel
{
    /// <summary>Bitcoin-style. Fees per kB.</summary>
    Utxo,

    /// <summary>Ethereum-style. Fees as gas prices.</summary>
    Account,
}

/// <summary>A supported chain and where BlockCypher serves it.</summary>
public sealed record NetworkInfo(BlockchainNetwork Network, string Coin, string Chain, ChainModel Model)
{
    /// <summary>Used in our URLs, e.g. btc-test3.</summary>
    public string Slug => $"{Coin}-{Chain}";

    /// <summary>BlockCypher path, e.g. btc/test3.</summary>
    public string Path => $"{Coin}/{Chain}";
}
