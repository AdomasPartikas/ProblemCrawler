using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProblemCrawler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddThreadSynthesis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ThreadSynthesisRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RootCollectorItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadItemCount = table.Column<int>(type: "integer", nullable: false),
                    AnalysedItemCount = table.Column<int>(type: "integer", nullable: false),
                    LatestCollectorItemCreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LatestAnalysedItemUpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Model = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AnalyzedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreadSynthesisRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThreadSynthesisRuns_CollectorItems_RootCollectorItemId",
                        column: x => x.RootCollectorItemId,
                        principalTable: "CollectorItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThreadSynthesizedIdeas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadSynthesisRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProblemSummary = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    ProblemDetails = table.Column<string>(type: "text", nullable: true),
                    Actor = table.Column<string>(type: "text", nullable: true),
                    Industry = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CurrentWorkaround = table.Column<string>(type: "text", nullable: true),
                    DesiredOutcome = table.Column<string>(type: "text", nullable: true),
                    UrgencySignal = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SoftwareOpportunity = table.Column<bool>(type: "boolean", nullable: false),
                    IsActionable = table.Column<bool>(type: "boolean", nullable: false),
                    ActionabilityRationale = table.Column<string>(type: "text", nullable: true),
                    SupportingMentionCount = table.Column<int>(type: "integer", nullable: false),
                    SupportingDistinctAuthorCount = table.Column<int>(type: "integer", nullable: false),
                    RawJson = table.Column<string>(type: "jsonb", nullable: false),
                    AnalyzedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreadSynthesizedIdeas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThreadSynthesizedIdeas_ThreadSynthesisRuns_ThreadSynthesisR~",
                        column: x => x.ThreadSynthesisRunId,
                        principalTable: "ThreadSynthesisRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ThreadSynthesisRuns_AnalyzedAtUtc",
                table: "ThreadSynthesisRuns",
                column: "AnalyzedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ThreadSynthesisRuns_RootCollectorItemId",
                table: "ThreadSynthesisRuns",
                column: "RootCollectorItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ThreadSynthesizedIdeas_AnalyzedAtUtc",
                table: "ThreadSynthesizedIdeas",
                column: "AnalyzedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ThreadSynthesizedIdeas_Industry",
                table: "ThreadSynthesizedIdeas",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_ThreadSynthesizedIdeas_IsActionable",
                table: "ThreadSynthesizedIdeas",
                column: "IsActionable");

            migrationBuilder.CreateIndex(
                name: "IX_ThreadSynthesizedIdeas_ThreadSynthesisRunId",
                table: "ThreadSynthesizedIdeas",
                column: "ThreadSynthesisRunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ThreadSynthesizedIdeas");

            migrationBuilder.DropTable(
                name: "ThreadSynthesisRuns");
        }
    }
}
