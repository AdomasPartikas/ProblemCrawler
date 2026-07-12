using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProblemCrawler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUmapProjection2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UmapProjectionEntity_ClusterRun_ClusterRunId",
                table: "UmapProjectionEntity");

            migrationBuilder.DropForeignKey(
                name: "FK_UmapProjectionEntity_ThreadSynthesizedIdeasEmbedding_Thread~",
                table: "UmapProjectionEntity");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UmapProjectionEntity",
                table: "UmapProjectionEntity");

            migrationBuilder.RenameTable(
                name: "UmapProjectionEntity",
                newName: "umapProjections");

            migrationBuilder.RenameIndex(
                name: "IX_UmapProjectionEntity_ThreadSynthesizedIdeaEmbeddingId",
                table: "umapProjections",
                newName: "IX_umapProjections_ThreadSynthesizedIdeaEmbeddingId");

            migrationBuilder.RenameIndex(
                name: "IX_UmapProjectionEntity_ClusterRunId_ThreadSynthesizedIdeaEmbe~",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
                newName: "UmapProjectionEntity");

            migrationBuilder.RenameIndex(
                name: "IX_umapProjections_ThreadSynthesizedIdeaEmbeddingId",
                table: "UmapProjectionEntity",
                newName: "IX_UmapProjectionEntity_ThreadSynthesizedIdeaEmbeddingId");

            migrationBuilder.RenameIndex(
                name: "IX_umapProjections_ClusterRunId_ThreadSynthesizedIdeaEmbedding~",
                table: "UmapProjectionEntity",
                newName: "IX_UmapProjectionEntity_ClusterRunId_ThreadSynthesizedIdeaEmbe~");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UmapProjectionEntity",
                table: "UmapProjectionEntity",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UmapProjectionEntity_ClusterRun_ClusterRunId",
                table: "UmapProjectionEntity",
                column: "ClusterRunId",
                principalTable: "ClusterRun",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UmapProjectionEntity_ThreadSynthesizedIdeasEmbedding_Thread~",
                table: "UmapProjectionEntity",
                column: "ThreadSynthesizedIdeaEmbeddingId",
                principalTable: "ThreadSynthesizedIdeasEmbedding",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
