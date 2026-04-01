using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProblemCrawler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysedItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalysedItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CollectorItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContainsProblem = table.Column<bool>(type: "boolean", nullable: false),
                    ProblemSummary = table.Column<string>(type: "text", nullable: true),
                    ExpandedProblem = table.Column<string>(type: "text", nullable: true),
                    Industry = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Actor = table.Column<string>(type: "text", nullable: true),
                    CurrentSolution = table.Column<string>(type: "text", nullable: true),
                    PainLevel = table.Column<int>(type: "integer", nullable: false),
                    FrequencySignal = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SoftwareOpportunity = table.Column<bool>(type: "boolean", nullable: false),
                    AutomationPotential = table.Column<bool>(type: "boolean", nullable: false),
                    IsB2B = table.Column<bool>(type: "boolean", nullable: false),
                    IsActionable = table.Column<bool>(type: "boolean", nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    RawJson = table.Column<string>(type: "jsonb", nullable: false),
                    Model = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AnalyzedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysedItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysedItems_CollectorItems_CollectorItemId",
                        column: x => x.CollectorItemId,
                        principalTable: "CollectorItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysedItems_AnalyzedAtUtc",
                table: "AnalysedItems",
                column: "AnalyzedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysedItems_CollectorItemId",
                table: "AnalysedItems",
                column: "CollectorItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalysedItems_Industry",
                table: "AnalysedItems",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysedItems_IsActionable",
                table: "AnalysedItems",
                column: "IsActionable");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalysedItems");
        }
    }
}
