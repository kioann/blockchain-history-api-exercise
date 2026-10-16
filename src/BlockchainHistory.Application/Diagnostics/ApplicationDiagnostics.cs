using System.Diagnostics;
using System.Diagnostics.Metrics;
using BlockchainHistory.Domain.Chains;

namespace BlockchainHistory.Application.Diagnostics;

/// <summary>The application's trace source and the name its meter is registered under.</summary>
public static class ApplicationDiagnostics
{
    public const string Name = "BlockchainHistory";

    public static readonly ActivitySource ActivitySource = new(Name);
}

/// <summary>Business metrics. Created through <see cref="IMeterFactory"/> so tests and hosts own the meter's lifetime.</summary>
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
        _snapshotsCaptured.Add(1, new KeyValuePair<string, object?>("network", BlockchainNetworks.Get(network).Slug));
}
