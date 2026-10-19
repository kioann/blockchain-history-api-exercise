namespace BlockchainHistory.Api.Extensions;

internal static class CorsExtensions
{
    public const string PolicyName = "frontend";
    private const string AllowedOriginsKey = "Cors:AllowedOrigins";

    /// <summary>Lets browser apps from Cors:AllowedOrigins call the API.</summary>
    public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection(AllowedOriginsKey).Get<string[]>() ?? [];

        return services.AddCors(options => options.AddPolicy(PolicyName, policy => policy
            .WithOrigins(origins)
            .WithMethods(HttpMethods.Get, HttpMethods.Post)
            .AllowAnyHeader()));
    }
}
