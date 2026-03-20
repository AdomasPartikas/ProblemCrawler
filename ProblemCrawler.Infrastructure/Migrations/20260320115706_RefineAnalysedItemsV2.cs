using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProblemCrawler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefineAnalysedItemsV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutomationPotential",
                table: "AnalysedItems");

            migrationBuilder.DropColumn(
                name: "IsB2B",
                table: "AnalysedItems");

            migrationBuilder.RenameColumn(
                name: "FrequencySignal",
                table: "AnalysedItems",
                newName: "UrgencySignal");

            migrationBuilder.RenameColumn(
                name: "ExpandedProblem",
                table: "AnalysedItems",
                newName: "ProblemDetails");

            migrationBuilder.RenameColumn(
                name: "CurrentSolution",
                table: "AnalysedItems",
                newName: "DesiredOutcome");

            migrationBuilder.AlterColumn<string>(
                name: "Industry",
                table: "AnalysedItems",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AddColumn<string>(
                name: "ActionabilityRationale",
                table: "AnalysedItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentWorkaround",
                table: "AnalysedItems",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActionabilityRationale",
                table: "AnalysedItems");

            migrationBuilder.DropColumn(
                name: "CurrentWorkaround",
                table: "AnalysedItems");

            migrationBuilder.RenameColumn(
                name: "UrgencySignal",
                table: "AnalysedItems",
                newName: "FrequencySignal");

            migrationBuilder.RenameColumn(
                name: "ProblemDetails",
                table: "AnalysedItems",
                newName: "ExpandedProblem");

            migrationBuilder.RenameColumn(
                name: "DesiredOutcome",
                table: "AnalysedItems",
                newName: "CurrentSolution");

            migrationBuilder.AlterColumn<string>(
                name: "Industry",
                table: "AnalysedItems",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<bool>(
                name: "AutomationPotential",
                table: "AnalysedItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsB2B",
                table: "AnalysedItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
