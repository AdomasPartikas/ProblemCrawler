using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProblemCrawler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUmapProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UmapProjectionEntity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClusterRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadSynthesizedIdeaEmbeddingId = table.Column<Guid>(type: "uuid", nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UmapProjectionEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UmapProjectionEntity_ClusterRun_ClusterRunId",
                        column: x => x.ClusterRunId,
                        principalTable: "ClusterRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UmapProjectionEntity_ThreadSynthesizedIdeasEmbedding_Thread~",
                        column: x => x.ThreadSynthesizedIdeaEmbeddingId,
                        principalTable: "ThreadSynthesizedIdeasEmbedding",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UmapProjectionEntity_ClusterRunId_ThreadSynthesizedIdeaEmbe~",
                table: "UmapProjectionEntity",
                columns: new[] { "ClusterRunId", "ThreadSynthesizedIdeaEmbeddingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UmapProjectionEntity_ThreadSynthesizedIdeaEmbeddingId",
                table: "UmapProjectionEntity",
                column: "ThreadSynthesizedIdeaEmbeddingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UmapProjectionEntity");
        }
    }
}
