using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCoinAndMarketModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LogoUrl",
                table: "Coins",
                newName: "Image");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Timestamp",
                table: "MarketOverviews",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<double>(
                name: "MarketCapChange",
                table: "MarketOverviews",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "VolumeChange",
                table: "MarketOverviews",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PriceChangePercentage24h",
                table: "Coins",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MarketCapChange",
                table: "MarketOverviews");

            migrationBuilder.DropColumn(
                name: "VolumeChange",
                table: "MarketOverviews");

            migrationBuilder.DropColumn(
                name: "PriceChangePercentage24h",
                table: "Coins");

            migrationBuilder.RenameColumn(
                name: "Image",
                table: "Coins",
                newName: "LogoUrl");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Timestamp",
                table: "MarketOverviews",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
