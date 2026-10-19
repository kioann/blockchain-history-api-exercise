using BlockchainHistory.Domain.Chains;
using BlockchainHistory.Domain.Snapshots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlockchainHistory.Infrastructure.Persistence.Configurations;

/// <summary>Both snapshot types in one table, with a ChainModel column to tell them apart.</summary>
internal sealed class BlockchainSnapshotConfiguration : IEntityTypeConfiguration<BlockchainSnapshot>
{
    public void Configure(EntityTypeBuilder<BlockchainSnapshot> builder)
    {
        builder.ToTable("Snapshots");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Ignore(s => s.DomainEvents);

        builder.HasDiscriminator<string>("ChainModel")
            .HasValue<UtxoChainSnapshot>(nameof(ChainModel.Utxo))
            .HasValue<EthereumSnapshot>(nameof(ChainModel.Account));
        builder.Property("ChainModel").HasMaxLength(16);

        builder.Property(s => s.Network).HasConversion<string>().HasMaxLength(16);
        builder.Property(s => s.Name).HasMaxLength(32);
        builder.Property(s => s.Hash).HasMaxLength(128);
        builder.Property(s => s.PreviousHash).HasMaxLength(128);
        builder.Property(s => s.LastForkHash).HasMaxLength(128);
        builder.Property(s => s.LatestUrl).HasMaxLength(512);
        builder.Property(s => s.PreviousUrl).HasMaxLength(512);
        builder.Property(s => s.RawJson).IsRequired();

        // Matches the history query: one chain, newest first.
        builder.HasIndex(s => new { s.Network, s.CreatedAt, s.Id })
            .IsDescending(false, true, true)
            .HasDatabaseName("IX_Snapshots_Network_CreatedAt");
    }
}
