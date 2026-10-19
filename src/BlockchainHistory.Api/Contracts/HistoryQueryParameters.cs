using System.ComponentModel.DataAnnotations;
using BlockchainHistory.Application.Snapshots.GetHistory;

namespace BlockchainHistory.Api.Contracts;

public sealed class HistoryQueryParameters
{
    /// <summary>Page number, starting at 1.</summary>
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    /// <summary>Items per page, 1 to 100.</summary>
    [Range(1, GetSnapshotHistoryQueryValidator.MaxPageSize)]
    public int PageSize { get; init; } = 20;
}

/// <summary>A supported chain.</summary>
/// <param name="Slug">Used in URLs, e.g. btc-main.</param>
/// <param name="Coin">BlockCypher coin code.</param>
/// <param name="Chain">BlockCypher chain name.</param>
/// <param name="Model">Utxo (fees per kB) or Account (gas prices).</param>
/// <param name="SourceUrl">Where the data comes from.</param>
public sealed record BlockchainResponse(string Slug, string Coin, string Chain, string Model, Uri SourceUrl);
