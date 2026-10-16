using BlockchainHistory.Domain.Chains;
using BlockchainHistory.Domain.Common;

namespace BlockchainHistory.Domain.Snapshots;

/// <summary>Raised when a new snapshot of a chain has been captured.</summary>
public sealed record SnapshotCaptured(Guid SnapshotId, BlockchainNetwork Network, long Height) : IDomainEvent;
