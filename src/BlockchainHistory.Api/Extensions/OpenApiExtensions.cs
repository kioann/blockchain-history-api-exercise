namespace BlockchainHistory.Api.Extensions;

internal static class OpenApiExtensions
{
    private const string Title = "Blockchain History API";
    private const string Version = "v1";
    private const string DocumentUrl = $"/openapi/{Version}.json";
    private const string SwaggerRoutePrefix = "swagger";
    private const string SwaggerEnabledKey = "Swagger:Enabled";

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services) =>
        services.AddOpenApi(Version, options => options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Title = Title;
            document.Info.Version = Version;
            document.Info.Description =
                "Captures chain status from BlockCypher (ETH, DASH, BTC, BTC testnet3, LTC) and serves each chain's history, newest first.";
            return Task.CompletedTask;
        }));

    /// <summary>Serves the OpenAPI document and Swagger UI at <c>/swagger</c>, unless <c>Swagger:Enabled</c> is false.</summary>
    public static WebApplication UseApiDocumentation(this WebApplication app)
    {
        if (!app.Configuration.GetValue(SwaggerEnabledKey, true))
        {
            return app;
        }

        app.MapOpenApi(DocumentUrl);
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(DocumentUrl, $"{Title} {Version}");
            options.RoutePrefix = SwaggerRoutePrefix;
        });
        app.MapGet("/", () => Results.Redirect($"/{SwaggerRoutePrefix}")).ExcludeFromDescription();

        return app;
    }
}
