using BlockchainHistory.Application.Abstractions;
using BlockchainHistory.Domain.Chains;
using BlockchainHistory.Domain.Snapshots;
using Mediator;
using Microsoft.Extensions.Logging;

namespace BlockchainHistory.Application.Snapshots.CaptureAll;

/// <summary>Fetch every supported network concurrently and store whatever succeeded.</summary>
public sealed record CaptureAllSnapshotsCommand : ICommand<CaptureAllResult>;

/// <summary>Snapshots that were stored, and networks that could not be fetched.</summary>
public sealed record CaptureAllResult(IReadOnlyList<SnapshotDto> Captured, IReadOnlyList<CaptureFailure> Failed);

public sealed record CaptureFailure(string Network, string Error);

public sealed partial class CaptureAllSnapshotsHandler(
    IBlockCypherClient client,
    ISnapshotRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<CaptureAllSnapshotsHandler> logger)
    : ICommandHandler<CaptureAllSnapshotsCommand, CaptureAllResult>
{
    public async ValueTask<CaptureAllResult> Handle(CaptureAllSnapshotsCommand command, CancellationToken cancellationToken)
    {
        // I/O-bound: start every request at once and await them together. No threads are blocked while waiting.
        var fetches = BlockchainNetworks.All.Select(n => FetchAsync(n.Network, cancellationToken));
        var outcomes = await Task.WhenAll(fetches);

        // The DbContext is not thread-safe, so snapshots are added one by one after all fetches complete.
        var createdAt = timeProvider.GetUtcNow();
        var captured = new List<SnapshotDto>();
        var failed = new List<CaptureFailure>();
        foreach (var (network, fetched, error) in outcomes)
        {
            if (fetched is null)
            {
                failed.Add(new CaptureFailure(BlockchainNetworks.Get(network).Slug, error!));
                continue;
            }

            var snapshot = BlockchainSnapshot.Capture(network, fetched.Status, fetched.RawJson, createdAt);
            repository.Add(snapshot);
            captured.Add(SnapshotMapper.ToDto(snapshot));
        }

        if (captured.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new CaptureAllResult(captured, failed);
    }

    private async Task<(BlockchainNetwork Network, ChainFetchResult? Fetched, string? Error)> FetchAsync(
        BlockchainNetwork network, CancellationToken cancellationToken)
    {
        try
        {
            return (network, await client.GetChainAsync(network, cancellationToken), null);
        }
        catch (BlockCypherUnavailableException ex)
        {
            LogFetchFailed(ex, BlockchainNetworks.Get(network).Slug);
            return (network, null, ex.Message);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not capture {Network}; continuing with the other networks")]
    private partial void LogFetchFailed(Exception exception, string network);
}
