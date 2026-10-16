using BlockchainHistory.Application.Abstractions;
using BlockchainHistory.Domain.Chains;
using BlockchainHistory.Domain.Snapshots;
using Mediator;

namespace BlockchainHistory.Application.Snapshots.Capture;

/// <summary>Fetch one network from BlockCypher now and store it as a snapshot.</summary>
public sealed record CaptureSnapshotCommand(BlockchainNetwork Network) : ICommand<SnapshotDto>;

public sealed class CaptureSnapshotHandler(
    IBlockCypherClient client, ISnapshotRepository repository, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : ICommandHandler<CaptureSnapshotCommand, SnapshotDto>
{
    public async ValueTask<SnapshotDto> Handle(CaptureSnapshotCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var fetched = await client.GetChainAsync(command.Network, cancellationToken);
        var snapshot = BlockchainSnapshot.Capture(
            command.Network, fetched.Status, fetched.RawJson, timeProvider.GetUtcNow());

        repository.Add(snapshot);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return SnapshotMapper.ToDto(snapshot);
    }
}
