using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// API gateway: one front door for the API. Routes live in config, so adding a service needs no code.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddHealthChecks();

// Starts each trace here, so one request shows up as gateway -> API -> BlockCypher.
var telemetry = builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("blockchain-history-gateway"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(o => o.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
        // Skip the gateway's own health checks of the API; they'd be a trace every 15 seconds.
        .AddHttpClientInstrumentation(o =>
            o.FilterHttpRequestMessage = request => request.RequestUri?.AbsolutePath.StartsWith("/health", StringComparison.Ordinal) != true));
if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
{
    telemetry.UseOtlpExporter();
}

var app = builder.Build();

app.MapHealthChecks("/health/live");
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapReverseProxy();

await app.RunAsync();
