using System.ComponentModel.DataAnnotations;
using BlockchainHistory.Application.Snapshots.GetHistory;

namespace BlockchainHistory.Api.Contracts;

/// <summary>Paging for history requests.</summary>
public sealed class HistoryQueryParameters
{
    /// <summary>1-based page number.</summary>
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    /// <summary>Items per page, 1 to 100.</summary>
    [Range(1, GetSnapshotHistoryQueryValidator.MaxPageSize)]
    public int PageSize { get; init; } = 20;
}

/// <summary>A supported network.</summary>
/// <param name="Slug">Identifier used in URLs, e.g. <c>btc-main</c>.</param>
/// <param name="Coin">BlockCypher coin code.</param>
/// <param name="Chain">BlockCypher chain name.</param>
/// <param name="Model">Utxo (fees per kB) or Account (gas prices).</param>
/// <param name="SourceUrl">The BlockCypher endpoint snapshots are captured from.</param>
public sealed record BlockchainResponse(string Slug, string Coin, string Chain, string Model, Uri SourceUrl);
