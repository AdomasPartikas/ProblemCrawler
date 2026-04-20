using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProblemCrawler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLabelToProblemCluster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ProblemCluster",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Label",
                table: "ProblemCluster",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Opportunity",
                table: "ProblemCluster",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "ProblemCluster");

            migrationBuilder.DropColumn(
                name: "Label",
                table: "ProblemCluster");

            migrationBuilder.DropColumn(
                name: "Opportunity",
                table: "ProblemCluster");
        }
    }
}
