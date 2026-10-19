namespace BlockchainHistory.Application.Abstractions;

/// <summary>Saves all changes in one go, then publishes their events.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
