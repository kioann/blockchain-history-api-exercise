using BlockchainHistory.Application.Abstractions;
using BlockchainHistory.Domain.Chains;
using Mediator;

namespace BlockchainHistory.Application.Snapshots.Capture;

/// <summary>Fetch one chain now and save it.</summary>
public sealed record CaptureSnapshotCommand(BlockchainNetwork Network) : ICommand<SnapshotDto>;

public sealed class CaptureSnapshotHandler(
    IBlockCypherClient client, ISnapshotRepository repository, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : ICommandHandler<CaptureSnapshotCommand, SnapshotDto>
{
    public async ValueTask<SnapshotDto> Handle(CaptureSnapshotCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Take the time first, so slow retries don't shift CreatedAt.
        var createdAt = timeProvider.GetUtcNow();
        var fetched = await client.GetChainAsync(command.Network, cancellationToken);

        var snapshot = repository.AddCaptured(command.Network, fetched, createdAt);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return snapshot;
    }
}
