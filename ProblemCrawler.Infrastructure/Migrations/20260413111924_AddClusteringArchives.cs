using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace ProblemCrawler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClusteringArchives : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProblemCluster_ClusterRun_Id",
                table: "ProblemCluster");

            migrationBuilder.AddColumn<Guid>(
                name: "ClusterRunId",
                table: "ProblemCluster",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "ClusterRun",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ProblemClusterArchive",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClusterRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClusterId = table.Column<int>(type: "integer", nullable: false),
                    Size = table.Column<int>(type: "integer", nullable: false),
                    AvgConfidence = table.Column<float>(type: "real", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProblemClusterArchive", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProblemClusterArchive_ClusterRun_ClusterRunId",
                        column: x => x.ClusterRunId,
                        principalTable: "ClusterRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ThreadSynthesizedIdeaEmbeddingArchive",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClusterRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadSynthesizedIdeaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector", nullable: false),
                    ClusterId = table.Column<int>(type: "integer", nullable: true),
                    ClusterConfidence = table.Column<float>(type: "real", nullable: true),
                    IdeaSnapshot = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreadSynthesizedIdeaEmbeddingArchive", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThreadSynthesizedIdeaEmbeddingArchive_ClusterRun_ClusterRun~",
                        column: x => x.ClusterRunId,
                        principalTable: "ClusterRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProblemCluster_ClusterRunId",
                table: "ProblemCluster",
                column: "ClusterRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ProblemClusterArchive_ClusterRunId",
                table: "ProblemClusterArchive",
                column: "ClusterRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ThreadSynthesizedIdeaEmbeddingArchive_ClusterRunId",
                table: "ThreadSynthesizedIdeaEmbeddingArchive",
                column: "ClusterRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ThreadSynthesizedIdeaEmbeddingArchive_ThreadSynthesizedIdea~",
                table: "ThreadSynthesizedIdeaEmbeddingArchive",
                column: "ThreadSynthesizedIdeaId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProblemCluster_ClusterRun_ClusterRunId",
                table: "ProblemCluster",
                column: "ClusterRunId",
                principalTable: "ClusterRun",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProblemCluster_ClusterRun_ClusterRunId",
                table: "ProblemCluster");

            migrationBuilder.DropTable(
                name: "ProblemClusterArchive");

            migrationBuilder.DropTable(
                name: "ThreadSynthesizedIdeaEmbeddingArchive");

            migrationBuilder.DropIndex(
                name: "IX_ProblemCluster_ClusterRunId",
                table: "ProblemCluster");

            migrationBuilder.DropColumn(
                name: "ClusterRunId",
                table: "ProblemCluster");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "ClusterRun");

            migrationBuilder.AddForeignKey(
                name: "FK_ProblemCluster_ClusterRun_Id",
                table: "ProblemCluster",
                column: "Id",
                principalTable: "ClusterRun",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
