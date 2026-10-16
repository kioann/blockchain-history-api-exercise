using BlockchainHistory.Application.Abstractions;
using BlockchainHistory.Domain.Chains;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace BlockchainHistory.FunctionalTests;

/// <summary>
/// Runs the whole API in memory against a throwaway SQLite file. BlockCypher and the clock are replaced
/// with test doubles, so tests are fast, offline and deterministic.
/// </summary>
public sealed class BlockchainHistoryApiFactory : WebApplicationFactory<Program>
{
    public const string AllowedOrigin = "https://allowed.example";

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"blockchain-history-test-{Guid.NewGuid():N}.db");

    /// <summary>High by default so only the rate-limit test ever hits it.</summary>
    public int CapturePermitsPerHour { get; init; } = 1000;

    public IBlockCypherClient BlockCypher { get; } = CreateBlockCypherStub();

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:BlockchainHistory", $"Data Source={_databasePath}");
        builder.UseSetting("RateLimiting:CapturePermitsPerHour", CapturePermitsPerHour.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.UseSetting("Cors:AllowedOrigins:0", AllowedOrigin);
        builder.UseSetting("Polling:Enabled", "false");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IBlockCypherClient>();
            services.AddSingleton(BlockCypher);
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
    }

    private static IBlockCypherClient CreateBlockCypherStub()
    {
        var client = Substitute.For<IBlockCypherClient>();
        client.GetChainAsync(Arg.Any<BlockchainNetwork>(), Arg.Any<CancellationToken>())
            .Returns(call => TestChainData.For(call.Arg<BlockchainNetwork>()));
        return client;
    }
}
