using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProblemCrawler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUmapProjection4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_umapProjections_ClusterRun_ClusterRunId",
                table: "umapProjections");

            migrationBuilder.DropForeignKey(
                name: "FK_umapProjections_ThreadSynthesizedIdeasEmbedding_ThreadSynth~",
                table: "umapProjections");

            migrationBuilder.DropPrimaryKey(
                name: "PK_umapProjections",
                table: "umapProjections");

            migrationBuilder.RenameTable(
                name: "umapProjections",
                newName: "UmapProjections");

            migrationBuilder.RenameIndex(
                name: "IX_umapProjections_ThreadSynthesizedIdeaEmbeddingId",
                table: "UmapProjections",
                newName: "IX_UmapProjections_ThreadSynthesizedIdeaEmbeddingId");

            migrationBuilder.RenameIndex(
                name: "IX_umapProjections_ClusterRunId_ThreadSynthesizedIdeaEmbedding~",
                table: "UmapProjections",
                newName: "IX_UmapProjections_ClusterRunId_ThreadSynthesizedIdeaEmbedding~");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UmapProjections",
                table: "UmapProjections",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UmapProjections_ClusterRun_ClusterRunId",
                table: "UmapProjections",
                column: "ClusterRunId",
                principalTable: "ClusterRun",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UmapProjections_ThreadSynthesizedIdeasEmbedding_ThreadSynth~",
                table: "UmapProjections",
                column: "ThreadSynthesizedIdeaEmbeddingId",
                principalTable: "ThreadSynthesizedIdeasEmbedding",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UmapProjections_ClusterRun_ClusterRunId",
                table: "UmapProjections");

            migrationBuilder.DropForeignKey(
                name: "FK_UmapProjections_ThreadSynthesizedIdeasEmbedding_ThreadSynth~",
                table: "UmapProjections");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UmapProjections",
                table: "UmapProjections");

            migrationBuilder.RenameTable(
                name: "UmapProjections",
                newName: "umapProjections");

            migrationBuilder.RenameIndex(
                name: "IX_UmapProjections_ThreadSynthesizedIdeaEmbeddingId",
                table: "umapProjections",
                newName: "IX_umapProjections_ThreadSynthesizedIdeaEmbeddingId");

            migrationBuilder.RenameIndex(
                name: "IX_UmapProjections_ClusterRunId_ThreadSynthesizedIdeaEmbedding~",
                table: "umapProjections",
                newName: "IX_umapProjections_ClusterRunId_ThreadSynthesizedIdeaEmbedding~");

            migrationBuilder.AddPrimaryKey(
                name: "PK_umapProjections",
                table: "umapProjections",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_umapProjections_ClusterRun_ClusterRunId",
                table: "umapProjections",
                column: "ClusterRunId",
                principalTable: "ClusterRun",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_umapProjections_ThreadSynthesizedIdeasEmbedding_ThreadSynth~",
                table: "umapProjections",
                column: "ThreadSynthesizedIdeaEmbeddingId",
                principalTable: "ThreadSynthesizedIdeasEmbedding",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
