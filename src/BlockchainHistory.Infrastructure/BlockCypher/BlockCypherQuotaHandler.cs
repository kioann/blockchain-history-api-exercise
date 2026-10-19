using System.Threading.RateLimiting;
using Polly.RateLimiting;

namespace BlockchainHistory.Infrastructure.BlockCypher;

/// <summary>
/// The account's request allowance, shared by every BlockCypher call in the process. Requests over the
/// per-second rate wait their turn; requests over the hourly budget are refused.
/// </summary>
internal sealed class BlockCypherQuota(BlockCypherOptions options) : IDisposable
{
    public RateLimiter PerSecond { get; } = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
    {
        TokenLimit = options.RequestsPerSecond,
        TokensPerPeriod = options.RequestsPerSecond,
        ReplenishmentPeriod = TimeSpan.FromSeconds(1),
        QueueLimit = 100,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
    });

    public RateLimiter PerHour { get; } = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
    {
        PermitLimit = options.RequestsPerHour,
        Window = TimeSpan.FromHours(1),
        QueueLimit = 0,
    });

    public void Dispose()
    {
        PerSecond.Dispose();
        PerHour.Dispose();
    }
}

/// <summary>
/// Runs inside the resilience pipeline, so every attempt is counted: user captures, scheduled captures and retries.
/// A refusal is not retried and surfaces as "BlockCypher unavailable".
/// </summary>
internal sealed class BlockCypherQuotaHandler(BlockCypherQuota quota) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var hourly = quota.PerHour.AttemptAcquire();
        if (!hourly.IsAcquired)
        {
            throw new RateLimiterRejectedException("The hourly BlockCypher request budget is used up.");
        }

        using var perSecond = await quota.PerSecond.AcquireAsync(1, cancellationToken);
        if (!perSecond.IsAcquired)
        {
            throw new RateLimiterRejectedException("Too many BlockCypher requests are already waiting.");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
