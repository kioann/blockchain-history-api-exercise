using System.Text.Json.Serialization;
using BlockchainHistory.Api.BackgroundServices;
using BlockchainHistory.Api.Errors;

namespace BlockchainHistory.Api.Extensions;

internal static class ApiServiceExtensions
{
    /// <summary>Controllers, JSON, error handling, OpenAPI, CORS, rate limiting and the optional poller.</summary>
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddApiDocumentation();
        services.AddFrontendCors(configuration);
        services.AddCaptureRateLimiting(configuration);
        services.AddSnapshotPolling(configuration);

        return services;
    }
}
