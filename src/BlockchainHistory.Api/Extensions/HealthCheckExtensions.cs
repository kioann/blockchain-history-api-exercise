using System.Net.Mime;
using System.Text.Json;
using BlockchainHistory.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BlockchainHistory.Api.Extensions;

internal static class HealthCheckExtensions
{
    public const string BasePath = "/health";

    /// <summary>
    /// /health/live: the app is running (no checks, so a slow database never restarts it).
    /// /health/ready: the database is reachable.
    /// </summary>
    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks($"{BasePath}/live", new HealthCheckOptions { Predicate = _ => false, ResponseWriter = WriteJsonAsync });
        app.MapHealthChecks($"{BasePath}/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(DependencyInjection.ReadinessTag),
            ResponseWriter = WriteJsonAsync,
        });
        return app;
    }

    private static Task WriteJsonAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = MediaTypeNames.Application.Json;
        return JsonSerializer.SerializeAsync(context.Response.Body, new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                durationMs = e.Value.Duration.TotalMilliseconds,
            }),
        }, cancellationToken: context.RequestAborted);
    }
}
