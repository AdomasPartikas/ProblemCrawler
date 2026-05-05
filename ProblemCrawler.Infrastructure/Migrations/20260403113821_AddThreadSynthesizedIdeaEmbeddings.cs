using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace ProblemCrawler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddThreadSynthesizedIdeaEmbeddings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "ThreadSynthesizedIdeasEmbedding",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadSynthesizedIdeaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(768)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreadSynthesizedIdeasEmbedding", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThreadSynthesizedIdeasEmbedding_ThreadSynthesizedIdeas_Thre~",
                        column: x => x.ThreadSynthesizedIdeaId,
                        principalTable: "ThreadSynthesizedIdeas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ThreadSynthesizedIdeasEmbedding_ThreadSynthesizedIdeaId",
                table: "ThreadSynthesizedIdeasEmbedding",
                column: "ThreadSynthesizedIdeaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThreadSynthesizedIdeasEmbedding_ThreadSynthesizedIdeaId_Mod~",
                table: "ThreadSynthesizedIdeasEmbedding",
                columns: new[] { "ThreadSynthesizedIdeaId", "Model" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ThreadSynthesizedIdeasEmbedding");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}
