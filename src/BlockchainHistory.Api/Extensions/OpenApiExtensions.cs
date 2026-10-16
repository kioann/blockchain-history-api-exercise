namespace BlockchainHistory.Api.Extensions;

internal static class OpenApiExtensions
{
    private const string DocumentUrl = "/openapi/v1.json";

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services) =>
        services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Title = "Blockchain History API";
            document.Info.Version = "v1";
            document.Info.Description =
                "Captures chain status from BlockCypher (ETH, DASH, BTC, BTC testnet3, LTC) and serves each chain's history, newest first.";
            return Task.CompletedTask;
        }));

    /// <summary>Serves the OpenAPI document and Swagger UI at <c>/swagger</c>, unless <c>Swagger:Enabled</c> is false.</summary>
    public static WebApplication UseApiDocumentation(this WebApplication app)
    {
        if (!app.Configuration.GetValue("Swagger:Enabled", true))
        {
            return app;
        }

        app.MapOpenApi(DocumentUrl);
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(DocumentUrl, "Blockchain History API v1");
            options.RoutePrefix = "swagger";
        });
        app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

        return app;
    }
}
