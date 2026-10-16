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

    /// <summary>Registers the database, repository, unit of work, BlockCypher client and the database health check.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName) ?? DefaultConnectionString;

        services.AddDbContext<BlockchainHistoryDbContext>(o => o.UseSqlite(connectionString));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BlockchainHistoryDbContext>());
        services.AddScoped<ISnapshotRepository, SnapshotRepository>();
        services.AddBlockCypherClient(configuration);
        services.AddHealthChecks().AddDbContextCheck<BlockchainHistoryDbContext>("database", tags: ["ready"]);

        return services;
    }

    /// <summary>Registers the typed BlockCypher client with retries (exponential + jitter), timeouts and a circuit breaker.</summary>
    public static IServiceCollection AddBlockCypherClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BlockCypherOptions>()
            .Bind(configuration.GetSection(BlockCypherOptions.SectionName))
            .Validate(o => o.IsValid(), "BlockCypher settings are invalid.")
            .ValidateOnStart();

        services
            .AddHttpClient<IBlockCypherClient, BlockCypherClient>((sp, http) =>
                http.BaseAddress = new Uri(sp.GetRequiredService<IOptions<BlockCypherOptions>>().Value.BaseUrl))
            .AddStandardResilienceHandler()
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

        return services;
    }

    /// <summary>Applies pending migrations, creating the SQLite file's folder first if needed.</summary>
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
