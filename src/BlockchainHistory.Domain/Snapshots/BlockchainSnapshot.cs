using BlockchainHistory.Domain.Chains;
using BlockchainHistory.Domain.Common;

namespace BlockchainHistory.Domain.Snapshots;

/// <summary>One saved reading of a chain. Shared fields live here; fees live in the subclasses.</summary>
public abstract class BlockchainSnapshot : Entity
{
    protected BlockchainSnapshot(BlockchainNetwork network, ChainStatus status, string rawJson, DateTimeOffset createdAt)
    {
        Id = Guid.CreateVersion7(createdAt);
        Network = network;
        Name = status.Name;
        Height = status.Height;
        Hash = status.Hash;
        Time = status.Time;
        LatestUrl = status.LatestUrl;
        PreviousHash = status.PreviousHash;
        PreviousUrl = status.PreviousUrl;
        PeerCount = status.PeerCount;
        UnconfirmedCount = status.UnconfirmedCount;
        LastForkHeight = status.LastForkHeight;
        LastForkHash = status.LastForkHash;
        RawJson = rawJson;
        CreatedAt = createdAt;
    }

    /// <summary>For EF Core.</summary>
    protected BlockchainSnapshot()
    {
    }

    public BlockchainNetwork Network { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public long Height { get; private set; }
    public string Hash { get; private set; } = string.Empty;
    public DateTimeOffset Time { get; private set; }
    public string LatestUrl { get; private set; } = string.Empty;
    public string PreviousHash { get; private set; } = string.Empty;
    public string PreviousUrl { get; private set; } = string.Empty;
    public long PeerCount { get; private set; }
    public long UnconfirmedCount { get; private set; }
    public long? LastForkHeight { get; private set; }
    public string? LastForkHash { get; private set; }

    /// <summary>BlockCypher's response, untouched.</summary>
    public string RawJson { get; private set; } = string.Empty;

    /// <summary>When we captured it (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Builds the right snapshot type for the chain.</summary>
    public static BlockchainSnapshot Capture(
        BlockchainNetwork network, ChainStatus status, string rawJson, DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(rawJson);

        BlockchainSnapshot snapshot = BlockchainNetworks.Get(network).Model switch
        {
            ChainModel.Account => new EthereumSnapshot(network, status, rawJson, createdAt),
            _ => new UtxoChainSnapshot(network, status, rawJson, createdAt),
        };

        snapshot.Raise(new SnapshotCaptured(snapshot.Id, network, snapshot.Height));
        return snapshot;
    }
}
