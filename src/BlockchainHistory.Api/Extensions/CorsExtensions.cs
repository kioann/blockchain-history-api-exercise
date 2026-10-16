namespace BlockchainHistory.Api.Extensions;

internal static class CorsExtensions
{
    public const string PolicyName = "frontend";

    /// <summary>Lets browser apps on the configured origins (<c>Cors:AllowedOrigins</c>) call the API.</summary>
    public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        return services.AddCors(options => options.AddPolicy(PolicyName, policy => policy
            .WithOrigins(origins)
            .WithMethods(HttpMethods.Get, HttpMethods.Post)
            .AllowAnyHeader()));
    }
}
