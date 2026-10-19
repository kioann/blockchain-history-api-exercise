using System.Diagnostics;
using System.Diagnostics.Metrics;
using BlockchainHistory.Domain.Chains;

namespace BlockchainHistory.Application.Diagnostics;

/// <summary>Names used for our traces and metrics.</summary>
public static class ApplicationDiagnostics
{
    public const string Name = "BlockchainHistory";

    public static readonly ActivitySource ActivitySource = new(Name);
}

/// <summary>Our business metrics.</summary>
public sealed class ApplicationMetrics
{
    private readonly Counter<long> _snapshotsCaptured;

    public ApplicationMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);

        var meter = meterFactory.Create(ApplicationDiagnostics.Name);
        _snapshotsCaptured = meter.CreateCounter<long>(
            "blockchain_history.snapshots.captured", unit: "{snapshot}", description: "Snapshots captured, per network.");
    }

    public void SnapshotCaptured(BlockchainNetwork network) =>
        _snapshotsCaptured.Add(1, new KeyValuePair<string, object?>("network", network.ToSlug()));
}
