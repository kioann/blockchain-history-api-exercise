using BlockchainHistory.Application.Abstractions;
using BlockchainHistory.Infrastructure.BlockCypher;
using BlockchainHistory.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace BlockchainHistory.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "BlockchainHistory";
    public const string DefaultConnectionString = "Data Source=data/blockchain-history.db";

    /// <summary>Checks with this tag decide whether we're ready.</summary>
    public const string ReadinessTag = "ready";

    /// <summary>Database, repository, BlockCypher client and the database health check.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName) ?? DefaultConnectionString;

        services.AddDbContext<BlockchainHistoryDbContext>(o => o.UseSqlite(connectionString));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BlockchainHistoryDbContext>());
        services.AddScoped<ISnapshotRepository, SnapshotRepository>();
        services.AddBlockCypherClient(configuration);
        services.AddHealthChecks().AddDbContextCheck<BlockchainHistoryDbContext>("database", tags: [ReadinessTag]);

        return services;
    }

    /// <summary>BlockCypher client with retries, timeouts, a circuit breaker and the quota.</summary>
    public static IServiceCollection AddBlockCypherClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BlockCypherOptions>()
            .Bind(configuration.GetSection(BlockCypherOptions.SectionName))
            .Validate(o => o.IsValid(), "BlockCypher settings are invalid.")
            .ValidateOnStart();
        services.AddSingleton(sp => new BlockCypherQuota(sp.GetRequiredService<IOptions<BlockCypherOptions>>().Value));
        services.AddTransient<BlockCypherQuotaHandler>();

        var client = services.AddHttpClient<IBlockCypherClient, BlockCypherClient>((sp, http) =>
            http.BaseAddress = new Uri(sp.GetRequiredService<IOptions<BlockCypherOptions>>().Value.BaseUrl));

        // Added after the resilience handler so it runs inside it and counts every retry.
        client.AddStandardResilienceHandler()
            .Configure((HttpStandardResilienceOptions resilience, IServiceProvider sp) =>
            {
                var o = sp.GetRequiredService<IOptions<BlockCypherOptions>>().Value;
                var attemptTimeout = TimeSpan.FromSeconds(o.TimeoutSeconds);

                resilience.Retry.MaxRetryAttempts = o.RetryCount;
                resilience.Retry.Delay = TimeSpan.FromMilliseconds(o.RetryDelayMilliseconds);
                resilience.AttemptTimeout.Timeout = attemptTimeout;
                resilience.TotalRequestTimeout.Timeout = (attemptTimeout * (o.RetryCount + 1)) + TimeSpan.FromSeconds(5);
                resilience.CircuitBreaker.SamplingDuration = attemptTimeout * 2 > TimeSpan.FromSeconds(30)
                    ? attemptTimeout * 2
                    : TimeSpan.FromSeconds(30);
            });
        client.AddHttpMessageHandler<BlockCypherQuotaHandler>();

        return services;
    }

    /// <summary>Creates the data folder if needed, then applies migrations.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BlockchainHistoryDbContext>();

        EnsureDataDirectoryExists(db.Database.GetConnectionString());
        await db.Database.MigrateAsync(cancellationToken);
    }

    private static void EnsureDataDirectoryExists(string? connectionString)
    {
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        if (string.IsNullOrEmpty(dataSource) || dataSource.StartsWith(':'))
        {
            return;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
