namespace BlockchainHistory.Application.Abstractions;

/// <summary>Commits all pending changes in one transaction and publishes their domain events.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
