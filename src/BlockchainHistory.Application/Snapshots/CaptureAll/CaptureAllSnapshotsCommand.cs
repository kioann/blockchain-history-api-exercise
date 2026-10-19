using BlockchainHistory.Application.Abstractions;
using BlockchainHistory.Domain.Chains;
using Mediator;
using Microsoft.Extensions.Logging;

namespace BlockchainHistory.Application.Snapshots.CaptureAll;

/// <summary>Fetch all chains at once and save the ones that worked.</summary>
public sealed record CaptureAllSnapshotsCommand : ICommand<CaptureAllResult>;

/// <summary>What was saved and what failed.</summary>
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
        var createdAt = timeProvider.GetUtcNow();

        // Network calls, so start them all and wait together. No threads sit blocked.
        var fetches = BlockchainNetworks.All.Select(n => FetchAsync(n.Network, cancellationToken));
        var outcomes = await Task.WhenAll(fetches);

        // DbContext isn't thread-safe, so add the results one by one.
        var captured = new List<SnapshotDto>();
        var failed = new List<CaptureFailure>();
        foreach (var (network, fetched, error) in outcomes)
        {
            if (fetched is null)
            {
                failed.Add(new CaptureFailure(network.ToSlug(), error!));
                continue;
            }

            captured.Add(repository.AddCaptured(network, fetched, createdAt));
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
            var slug = network.ToSlug();
            LogFetchFailed(ex, slug);
            return (network, null, ex.Message);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not capture {Network}; continuing with the other networks")]
    private partial void LogFetchFailed(Exception exception, string network);
}
