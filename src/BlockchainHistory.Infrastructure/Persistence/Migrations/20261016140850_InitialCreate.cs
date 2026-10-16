using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlockchainHistory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Network = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Height = table.Column<long>(type: "INTEGER", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Time = table.Column<long>(type: "INTEGER", nullable: false),
                    LatestUrl = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    PreviousHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    PreviousUrl = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    PeerCount = table.Column<long>(type: "INTEGER", nullable: false),
                    UnconfirmedCount = table.Column<long>(type: "INTEGER", nullable: false),
                    LastForkHeight = table.Column<long>(type: "INTEGER", nullable: true),
                    LastForkHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    RawJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    ChainModel = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    HighGasPrice = table.Column<long>(type: "INTEGER", nullable: true),
                    MediumGasPrice = table.Column<long>(type: "INTEGER", nullable: true),
                    LowGasPrice = table.Column<long>(type: "INTEGER", nullable: true),
                    HighPriorityFee = table.Column<long>(type: "INTEGER", nullable: true),
                    MediumPriorityFee = table.Column<long>(type: "INTEGER", nullable: true),
                    LowPriorityFee = table.Column<long>(type: "INTEGER", nullable: true),
                    BaseFee = table.Column<long>(type: "INTEGER", nullable: true),
                    HighFeePerKb = table.Column<long>(type: "INTEGER", nullable: true),
                    MediumFeePerKb = table.Column<long>(type: "INTEGER", nullable: true),
                    LowFeePerKb = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Snapshots_Network_CreatedAt",
                table: "Snapshots",
                columns: new[] { "Network", "CreatedAt", "Id" },
                descending: new[] { false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Snapshots");
        }
    }
}
