using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProblemCrawler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveThreadRootToAnalysedRootCollector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RootCollectorItemId",
                table: "AnalysedItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "AnalysedItems" AS analysed
                SET "RootCollectorItemId" = COALESCE(root."Id", current_item."Id")
                FROM "CollectorItems" AS current_item
                LEFT JOIN "CollectorItems" AS root
                    ON root."Source" = current_item."Source"
                   AND root."SourceId" = CASE
                        WHEN current_item."ItemType" = 'Comment' AND COALESCE(NULLIF(current_item."LinkId", ''), '') <> '' THEN current_item."LinkId"
                        ELSE current_item."SourceId"
                   END
                WHERE analysed."CollectorItemId" = current_item."Id";
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "RootCollectorItemId",
                table: "AnalysedItems",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalysedItems_RootCollectorItemId",
                table: "AnalysedItems",
                column: "RootCollectorItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalysedItems_CollectorItems_RootCollectorItemId",
                table: "AnalysedItems",
                column: "RootCollectorItemId",
                principalTable: "CollectorItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropIndex(
                name: "IX_CollectorItems_Source_ThreadRootSourceId",
                table: "CollectorItems");

            migrationBuilder.DropColumn(
                name: "ThreadRootSourceId",
                table: "CollectorItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalysedItems_CollectorItems_RootCollectorItemId",
                table: "AnalysedItems");

            migrationBuilder.DropIndex(
                name: "IX_AnalysedItems_RootCollectorItemId",
                table: "AnalysedItems");

            migrationBuilder.DropColumn(
                name: "RootCollectorItemId",
                table: "AnalysedItems");

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
    }
}
