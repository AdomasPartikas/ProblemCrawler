using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProblemCrawler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClustering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "ClusterConfidence",
                table: "ThreadSynthesizedIdeasEmbedding",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClusterId",
                table: "ThreadSynthesizedIdeasEmbedding",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClusterRunId",
                table: "ThreadSynthesizedIdeasEmbedding",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "ThreadSynthesizedIdeasEmbedding",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "ClusterRun",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Algorithm = table.Column<string>(type: "text", nullable: false),
                    MinClusterSize = table.Column<int>(type: "integer", nullable: false),
                    MinSamples = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterRun", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProblemCluster",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClusterId = table.Column<int>(type: "integer", nullable: false),
                    Size = table.Column<int>(type: "integer", nullable: false),
                    AvgConfidence = table.Column<float>(type: "real", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProblemCluster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProblemCluster_ClusterRun_Id",
                        column: x => x.Id,
                        principalTable: "ClusterRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ThreadSynthesizedIdeasEmbedding_ClusterRunId",
                table: "ThreadSynthesizedIdeasEmbedding",
                column: "ClusterRunId");

            migrationBuilder.AddForeignKey(
                name: "FK_ThreadSynthesizedIdeasEmbedding_ClusterRun_ClusterRunId",
                table: "ThreadSynthesizedIdeasEmbedding",
                column: "ClusterRunId",
                principalTable: "ClusterRun",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ThreadSynthesizedIdeasEmbedding_ClusterRun_ClusterRunId",
                table: "ThreadSynthesizedIdeasEmbedding");

            migrationBuilder.DropTable(
                name: "ProblemCluster");

            migrationBuilder.DropTable(
                name: "ClusterRun");

            migrationBuilder.DropIndex(
                name: "IX_ThreadSynthesizedIdeasEmbedding_ClusterRunId",
                table: "ThreadSynthesizedIdeasEmbedding");

            migrationBuilder.DropColumn(
                name: "ClusterConfidence",
                table: "ThreadSynthesizedIdeasEmbedding");

            migrationBuilder.DropColumn(
                name: "ClusterId",
                table: "ThreadSynthesizedIdeasEmbedding");

            migrationBuilder.DropColumn(
                name: "ClusterRunId",
                table: "ThreadSynthesizedIdeasEmbedding");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "ThreadSynthesizedIdeasEmbedding");
        }
    }
}
