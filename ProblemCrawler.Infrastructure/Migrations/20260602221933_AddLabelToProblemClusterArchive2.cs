using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProblemCrawler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLabelToProblemClusterArchive2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ProblemClusterArchive",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Label",
                table: "ProblemClusterArchive",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Opportunity",
                table: "ProblemClusterArchive",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "ProblemClusterArchive");

            migrationBuilder.DropColumn(
                name: "Label",
                table: "ProblemClusterArchive");

            migrationBuilder.DropColumn(
                name: "Opportunity",
                table: "ProblemClusterArchive");
        }
    }
}
