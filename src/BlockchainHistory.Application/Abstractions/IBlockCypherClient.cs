using BlockchainHistory.Domain.Chains;
using BlockchainHistory.Domain.Snapshots;

namespace BlockchainHistory.Application.Abstractions;

/// <summary>Reads the current state of a chain from BlockCypher.</summary>
public interface IBlockCypherClient
{
    /// <exception cref="BlockCypherUnavailableException">The provider could not be reached or kept failing.</exception>
    Task<ChainFetchResult> GetChainAsync(BlockchainNetwork network, CancellationToken cancellationToken);
}

/// <summary>The parsed chain state together with the response body exactly as received.</summary>
public sealed record ChainFetchResult(ChainStatus Status, string RawJson);
