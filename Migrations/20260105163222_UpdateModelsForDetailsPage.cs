using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelsForDetailsPage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MarketStatistics",
                table: "MarketStatistics");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "MarketStatistics",
                newName: "MarketCapRank");

            migrationBuilder.AlterColumn<int>(
                name: "MarketCapRank",
                table: "MarketStatistics",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<double>(
                name: "CirculatingSupply",
                table: "MarketStatistics",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentPrice",
                table: "MarketStatistics",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "High24h",
                table: "MarketStatistics",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Low24h",
                table: "MarketStatistics",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "MarketCap",
                table: "MarketStatistics",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<double>(
                name: "MaxSupply",
                table: "MarketStatistics",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PriceChangePercentage24h",
                table: "MarketStatistics",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TotalSupply",
                table: "MarketStatistics",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalVolume",
                table: "MarketStatistics",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Coins",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MarketStatistics",
                table: "MarketStatistics",
                column: "CoinId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MarketStatistics",
                table: "MarketStatistics");

            migrationBuilder.DropColumn(
                name: "CirculatingSupply",
                table: "MarketStatistics");

            migrationBuilder.DropColumn(
                name: "CurrentPrice",
                table: "MarketStatistics");

            migrationBuilder.DropColumn(
                name: "High24h",
                table: "MarketStatistics");

            migrationBuilder.DropColumn(
                name: "Low24h",
                table: "MarketStatistics");

            migrationBuilder.DropColumn(
                name: "MarketCap",
                table: "MarketStatistics");

            migrationBuilder.DropColumn(
                name: "MaxSupply",
                table: "MarketStatistics");

            migrationBuilder.DropColumn(
                name: "PriceChangePercentage24h",
                table: "MarketStatistics");

            migrationBuilder.DropColumn(
                name: "TotalSupply",
                table: "MarketStatistics");

            migrationBuilder.DropColumn(
                name: "TotalVolume",
                table: "MarketStatistics");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Coins");

            migrationBuilder.RenameColumn(
                name: "MarketCapRank",
                table: "MarketStatistics",
                newName: "Id");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "MarketStatistics",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MarketStatistics",
                table: "MarketStatistics",
                column: "Id");
        }
    }
}
