using BlockchainHistory.Domain.Chains;

namespace BlockchainHistory.Domain.Snapshots;

/// <summary>Snapshot of an Ethereum-style chain. Gas prices and fees are in wei.</summary>
public sealed class EthereumSnapshot : BlockchainSnapshot
{
    internal EthereumSnapshot(BlockchainNetwork network, ChainStatus status, string rawJson, DateTimeOffset createdAt)
        : base(network, status, rawJson, createdAt)
    {
        HighGasPrice = status.HighGasPrice;
        MediumGasPrice = status.MediumGasPrice;
        LowGasPrice = status.LowGasPrice;
        HighPriorityFee = status.HighPriorityFee;
        MediumPriorityFee = status.MediumPriorityFee;
        LowPriorityFee = status.LowPriorityFee;
        BaseFee = status.BaseFee;
    }

    private EthereumSnapshot()
    {
    }

    public long? HighGasPrice { get; private set; }
    public long? MediumGasPrice { get; private set; }
    public long? LowGasPrice { get; private set; }
    public long? HighPriorityFee { get; private set; }
    public long? MediumPriorityFee { get; private set; }
    public long? LowPriorityFee { get; private set; }
    public long? BaseFee { get; private set; }
}
