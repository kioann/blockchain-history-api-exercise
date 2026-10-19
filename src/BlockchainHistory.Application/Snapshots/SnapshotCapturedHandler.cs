using BlockchainHistory.Application.Diagnostics;
using BlockchainHistory.Domain.Chains;
using BlockchainHistory.Domain.Snapshots;
using Mediator;
using Microsoft.Extensions.Logging;

namespace BlockchainHistory.Application.Snapshots;

/// <summary>Reacts to a stored snapshot: counts it in the metrics and logs it.</summary>
public sealed partial class SnapshotCapturedHandler(ApplicationMetrics metrics, ILogger<SnapshotCapturedHandler> logger)
    : INotificationHandler<SnapshotCaptured>
{
    public ValueTask Handle(SnapshotCaptured notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        metrics.SnapshotCaptured(notification.Network);
        var slug = notification.Network.ToSlug();
        LogCaptured(slug, notification.Height, notification.SnapshotId);
        return ValueTask.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Captured {Network} at height {Height} as {SnapshotId}")]
    private partial void LogCaptured(string network, long height, Guid snapshotId);
}
