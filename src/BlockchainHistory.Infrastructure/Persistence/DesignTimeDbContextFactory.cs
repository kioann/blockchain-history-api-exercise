using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BlockchainHistory.Infrastructure.Persistence;

/// <summary>Lets dotnet ef build migrations without starting the API.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BlockchainHistoryDbContext>
{
    public BlockchainHistoryDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<BlockchainHistoryDbContext>()
                .UseSqlite(DependencyInjection.DefaultConnectionString)
                .Options,
            new NoOpPublisher());

    private sealed class NoOpPublisher : IPublisher
    {
        public ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => ValueTask.CompletedTask;

        public ValueTask Publish(object notification, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;
    }
}
