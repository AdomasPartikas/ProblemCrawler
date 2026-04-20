using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProblemCrawler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSynthesisTrackingToAnalysedItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSynthesisInProgress",
                table: "AnalysedItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynthesized",
                table: "AnalysedItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SynthesisClaimedAtUtc",
                table: "AnalysedItems",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSynthesisInProgress",
                table: "AnalysedItems");

            migrationBuilder.DropColumn(
                name: "IsSynthesized",
                table: "AnalysedItems");

            migrationBuilder.DropColumn(
                name: "SynthesisClaimedAtUtc",
                table: "AnalysedItems");
        }
    }
}
