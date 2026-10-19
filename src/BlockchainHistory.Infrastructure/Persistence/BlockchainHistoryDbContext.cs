using BlockchainHistory.Application.Abstractions;
using BlockchainHistory.Domain.Common;
using BlockchainHistory.Domain.Snapshots;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BlockchainHistory.Infrastructure.Persistence;

/// <summary>EF Core context and unit of work. Saves everything, then publishes the domain events.</summary>
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

        // Publish after saving, so handlers never see unsaved data.
        entities.ForEach(e => e.ClearDomainEvents());
        foreach (object domainEvent in domainEvents)
        {
            // The object overload routes by the event's real type.
            await publisher.Publish(domainEvent, cancellationToken);
        }

        return written;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BlockchainHistoryDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        // SQLite can't sort DateTimeOffset. UTC ticks keep full precision and sort in the database.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcTicksConverter>();
    }

    private sealed class UtcTicksConverter() : ValueConverter<DateTimeOffset, long>(
        value => value.UtcTicks,
        ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
}
