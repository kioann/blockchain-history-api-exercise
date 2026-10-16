namespace BlockchainHistory.Infrastructure.BlockCypher;

/// <summary>Settings bound from the <c>BlockCypher</c> configuration section.</summary>
public sealed class BlockCypherOptions
{
    public const string SectionName = "BlockCypher";

    public string BaseUrl { get; set; } = "https://api.blockcypher.com/v1/";

    /// <summary>Optional API token. Supply it via user-secrets or the BlockCypher__Token environment variable.</summary>
    public string? Token { get; set; }

    /// <summary>Timeout for a single attempt.</summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>Retries after the first attempt, for transient failures (5xx, 408, 429, timeouts).</summary>
    public int RetryCount { get; set; } = 2;

    /// <summary>Base delay between retries. Grows exponentially, with jitter.</summary>
    public int RetryDelayMilliseconds { get; set; } = 500;

    internal bool IsValid() =>
        Uri.TryCreate(BaseUrl, UriKind.Absolute, out _) && TimeoutSeconds > 0 && RetryCount >= 0 && RetryDelayMilliseconds >= 0;
}
