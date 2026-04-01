using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProblemCrawler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddThreadRootSourceIdToCollectorItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThreadRootSourceId",
                table: "CollectorItems",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "CollectorItems"
                SET "ThreadRootSourceId" = CASE
                    WHEN "ItemType" = 'Comment' AND COALESCE(NULLIF("LinkId", ''), '') <> '' THEN "LinkId"
                    ELSE "SourceId"
                END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ThreadRootSourceId",
                table: "CollectorItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollectorItems_Source_ThreadRootSourceId",
                table: "CollectorItems",
                columns: new[] { "Source", "ThreadRootSourceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CollectorItems_Source_ThreadRootSourceId",
                table: "CollectorItems");

            migrationBuilder.DropColumn(
                name: "ThreadRootSourceId",
                table: "CollectorItems");
        }
    }
}
