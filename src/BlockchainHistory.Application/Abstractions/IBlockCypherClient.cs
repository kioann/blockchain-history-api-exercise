using BlockchainHistory.Domain.Chains;
using BlockchainHistory.Domain.Snapshots;

namespace BlockchainHistory.Application.Abstractions;

/// <summary>Gets a chain's current state from BlockCypher.</summary>
public interface IBlockCypherClient
{
    /// <exception cref="BlockCypherUnavailableException">BlockCypher couldn't be reached or kept failing.</exception>
    Task<ChainFetchResult> GetChainAsync(BlockchainNetwork network, CancellationToken cancellationToken);
}

/// <summary>Parsed data plus the raw response.</summary>
public sealed record ChainFetchResult(ChainStatus Status, string RawJson);
