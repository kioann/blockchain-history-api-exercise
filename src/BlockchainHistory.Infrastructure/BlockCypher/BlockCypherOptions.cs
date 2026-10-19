using BlockchainHistory.Domain.Chains;

namespace BlockchainHistory.Infrastructure.BlockCypher;

/// <summary>Settings from the BlockCypher config section.</summary>
public sealed class BlockCypherOptions
{
    public const string SectionName = "BlockCypher";

    /// <summary>API root, ending with a slash.</summary>
    public string BaseUrl { get; set; } = "https://api.blockcypher.com/v1/";

    /// <summary>Optional. Set it with user-secrets or BlockCypher__Token, never in a file.</summary>
    public string? Token { get; set; }

    /// <summary>Timeout per attempt.</summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>Extra attempts on temporary errors (5xx, 429, timeouts).</summary>
    public int RetryCount { get; set; } = 2;

    /// <summary>First retry delay. Grows with each retry, with jitter.</summary>
    public int RetryDelayMilliseconds { get; set; } = 500;

    /// <summary>Free tier: 3. Extra requests wait.</summary>
    public int RequestsPerSecond { get; set; } = 3;

    /// <summary>Free tier: 100. Extra requests are refused.</summary>
    public int RequestsPerHour { get; set; } = 100;

    /// <summary>Where a chain's data comes from.</summary>
    public Uri SourceUrl(NetworkInfo network)
    {
        ArgumentNullException.ThrowIfNull(network);
        return new Uri(new Uri(BaseUrl), network.Path);
    }

    internal bool IsValid() =>
        Uri.TryCreate(BaseUrl, UriKind.Absolute, out _) && BaseUrl.EndsWith('/')
        && TimeoutSeconds > 0 && RetryCount >= 1 && RetryDelayMilliseconds >= 0
        && RequestsPerSecond > 0 && RequestsPerHour > 0;
}
