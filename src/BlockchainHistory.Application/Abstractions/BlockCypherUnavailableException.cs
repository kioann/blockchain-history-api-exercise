using BlockchainHistory.Domain.Chains;

namespace BlockchainHistory.Application.Abstractions;

/// <summary>BlockCypher could not provide data for a network, even after retries.</summary>
public sealed class BlockCypherUnavailableException : Exception
{
    public BlockCypherUnavailableException(BlockchainNetwork network, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Network = network;
    }

    public BlockchainNetwork Network { get; }
}
