using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProblemCrawler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClusterValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClusterValidation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClusterRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClusterId = table.Column<int>(type: "integer", nullable: false),
                    CoherenceRating = table.Column<int>(type: "integer", nullable: false),
                    NoveltyRating = table.Column<int>(type: "integer", nullable: false),
                    ProductPotentialRating = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterValidation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClusterValidation_ClusterRun_ClusterRunId",
                        column: x => x.ClusterRunId,
                        principalTable: "ClusterRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClusterValidation_ClusterRunId_ClusterId",
                table: "ClusterValidation",
                columns: new[] { "ClusterRunId", "ClusterId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClusterValidation");
        }
    }
}
