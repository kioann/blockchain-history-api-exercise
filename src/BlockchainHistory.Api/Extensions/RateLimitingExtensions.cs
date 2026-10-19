using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace BlockchainHistory.Api.Extensions;

internal static class RateLimitingExtensions
{
    public const string CapturePolicy = "capture";
    private const string PermitsPerHourKey = "RateLimiting:CapturePermitsPerHour";
    private const int DefaultPermitsPerHour = 20;

    /// <summary>
    /// One shared hourly budget for API clients on the capture endpoints, so a single caller cannot use up
    /// the BlockCypher quota. The quota itself is enforced on every outgoing call by the BlockCypher client.
    /// </summary>
    public static IServiceCollection AddCaptureRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var permitsPerHour = configuration.GetValue(PermitsPerHourKey, DefaultPermitsPerHour);

        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter(CapturePolicy, limiter =>
            {
                limiter.PermitLimit = permitsPerHour;
                limiter.Window = TimeSpan.FromHours(1);
                limiter.QueueLimit = 0;
            });
            options.OnRejected = WriteRejectionAsync;
        });
    }

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        var problems = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problems.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context.HttpContext,
            ProblemDetails =
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Capture budget exhausted",
                Detail = "Captures are limited to protect the upstream provider's hourly quota. Try again later.",
            },
        });
    }
}
