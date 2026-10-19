using BlockchainHistory.Infrastructure.Persistence;
using Mediator;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace BlockchainHistory.IntegrationTests.Persistence;

/// <summary>A real in-memory SQLite database for one test. The connection stays open, or the database disappears.</summary>
internal sealed class SqliteDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    private SqliteDatabase()
    {
    }

    public IPublisher Publisher { get; } = Substitute.For<IPublisher>();

    public static async Task<SqliteDatabase> CreateAsync(CancellationToken cancellationToken)
    {
        var database = new SqliteDatabase();
        await database._connection.OpenAsync(cancellationToken);
        await using var context = database.NewContext();
        await context.Database.MigrateAsync(cancellationToken);
        return database;
    }

    public BlockchainHistoryDbContext NewContext() =>
        new(new DbContextOptionsBuilder<BlockchainHistoryDbContext>().UseSqlite(_connection).Options, Publisher);

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}
