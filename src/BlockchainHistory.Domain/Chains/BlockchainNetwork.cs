namespace BlockchainHistory.Domain.Chains;

/// <summary>The blockchain networks this service tracks.</summary>
public enum BlockchainNetwork
{
    EthMain,
    DashMain,
    BtcMain,
    BtcTest3,
    LtcMain,
}

/// <summary>How a chain models balances, which decides how its fees are expressed.</summary>
public enum ChainModel
{
    /// <summary>Bitcoin-style unspent outputs. Fees are quoted per kilobyte.</summary>
    Utxo,

    /// <summary>Ethereum-style accounts. Fees are quoted as gas prices.</summary>
    Account,
}

/// <summary>Static facts about a network: its public slug, BlockCypher path and chain model.</summary>
public sealed record NetworkInfo(BlockchainNetwork Network, string Slug, string Coin, string Chain, ChainModel Model);
