using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Story35OutdatedTranslationDetection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOutdated",
                table: "content_item_language_tasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PreviousApprovedTranslation",
                table: "content_item_language_tasks",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_content_item_language_tasks_IsOutdated",
                table: "content_item_language_tasks",
                column: "IsOutdated");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_content_item_language_tasks_IsOutdated",
                table: "content_item_language_tasks");

            migrationBuilder.DropColumn(
                name: "IsOutdated",
                table: "content_item_language_tasks");

            migrationBuilder.DropColumn(
                name: "PreviousApprovedTranslation",
                table: "content_item_language_tasks");
        }
    }
}

