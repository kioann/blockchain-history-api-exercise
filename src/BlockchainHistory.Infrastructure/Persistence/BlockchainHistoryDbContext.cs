using BlockchainHistory.Application.Abstractions;
using BlockchainHistory.Domain.Common;
using BlockchainHistory.Domain.Snapshots;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BlockchainHistory.Infrastructure.Persistence;

/// <summary>
/// EF Core session over the SQLite database. Acts as the unit of work: one SaveChanges commits everything
/// pending, then publishes the domain events raised by the saved entities.
/// </summary>
public sealed class BlockchainHistoryDbContext(DbContextOptions<BlockchainHistoryDbContext> options, IPublisher publisher)
    : DbContext(options), IUnitOfWork
{
    public DbSet<BlockchainSnapshot> Snapshots => Set<BlockchainSnapshot>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entities = ChangeTracker.Entries<Entity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();
        var domainEvents = entities.SelectMany(e => e.DomainEvents).ToList();

        var written = await base.SaveChangesAsync(cancellationToken);

        // Publish only after the data is committed, so handlers never observe changes that were rolled back.
        entities.ForEach(e => e.ClearDomainEvents());
        foreach (object domainEvent in domainEvents)
        {
            // The object overload dispatches on the event's runtime type to its handlers.
            await publisher.Publish(domainEvent, cancellationToken);
        }

        return written;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BlockchainHistoryDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        // SQLite has no native DateTimeOffset and cannot ORDER BY one. Storing UTC ticks keeps full precision
        // and lets "newest first" queries sort in the database instead of in memory.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcTicksConverter>();
    }

    private sealed class UtcTicksConverter() : ValueConverter<DateTimeOffset, long>(
        value => value.UtcTicks,
        ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
}
