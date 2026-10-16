using BlockchainHistory.Application.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace BlockchainHistory.Api.Extensions;

internal static class ObservabilityExtensions
{
    /// <summary>
    /// OpenTelemetry traces, metrics and logs. Exported over OTLP when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is set
    /// (for example to the Aspire dashboard in docker-compose), otherwise collected but not exported.
    /// </summary>
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        var telemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("blockchain-history-api"))
            .WithTracing(tracing => tracing
                .AddSource(ApplicationDiagnostics.Name)
                .AddAspNetCoreInstrumentation(o => o.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
                .AddHttpClientInstrumentation())
            .WithMetrics(metrics => metrics
                .AddMeter(ApplicationDiagnostics.Name)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation());

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            telemetry.UseOtlpExporter();
        }

        return builder;
    }
}
