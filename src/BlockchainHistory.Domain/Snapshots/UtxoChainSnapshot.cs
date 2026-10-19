using BlockchainHistory.Domain.Chains;

namespace BlockchainHistory.Domain.Snapshots;

/// <summary>Bitcoin-style snapshot (BTC, LTC, DASH). Fees are per kB.</summary>
public sealed class UtxoChainSnapshot : BlockchainSnapshot
{
    internal UtxoChainSnapshot(BlockchainNetwork network, ChainStatus status, string rawJson, DateTimeOffset createdAt)
        : base(network, status, rawJson, createdAt)
    {
        HighFeePerKb = status.HighFeePerKb;
        MediumFeePerKb = status.MediumFeePerKb;
        LowFeePerKb = status.LowFeePerKb;
    }

    private UtxoChainSnapshot()
    {
    }

    public long? HighFeePerKb { get; private set; }
    public long? MediumFeePerKb { get; private set; }
    public long? LowFeePerKb { get; private set; }
}
