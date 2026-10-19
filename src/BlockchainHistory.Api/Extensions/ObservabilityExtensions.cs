using BlockchainHistory.Application.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace BlockchainHistory.Api.Extensions;

internal static class ObservabilityExtensions
{
    private const string ServiceName = "blockchain-history-api";
    private const string OtlpEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";

    /// <summary>Traces, metrics and logs via OpenTelemetry. Sent over OTLP when OTEL_EXPORTER_OTLP_ENDPOINT is set.</summary>
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        var telemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing => tracing
                .AddSource(ApplicationDiagnostics.Name)
                .AddAspNetCoreInstrumentation(o =>
                    o.Filter = context => !context.Request.Path.StartsWithSegments(HealthCheckExtensions.BasePath))
                .AddHttpClientInstrumentation())
            .WithMetrics(metrics => metrics
                .AddMeter(ApplicationDiagnostics.Name)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation());

        if (!string.IsNullOrWhiteSpace(builder.Configuration[OtlpEndpointKey]))
        {
            telemetry.UseOtlpExporter();
        }

        return builder;
    }
}
