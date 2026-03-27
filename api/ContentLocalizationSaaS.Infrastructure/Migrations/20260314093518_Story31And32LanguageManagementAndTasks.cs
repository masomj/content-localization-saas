using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Story31And32LanguageManagementAndTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "content_item_language_tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: false),
                    AssigneeEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false, defaultValue: ""),
                    DueUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "todo"),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_item_language_tasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "project_languages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Bcp47Code = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: false),
                    IsSource = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_languages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_content_item_language_tasks_ContentItemId_LanguageCode",
                table: "content_item_language_tasks",
                columns: new[] { "ContentItemId", "LanguageCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_content_item_language_tasks_DueUtc",
                table: "content_item_language_tasks",
                column: "DueUtc");

            migrationBuilder.CreateIndex(
                name: "IX_project_languages_ProjectId_Bcp47Code",
                table: "project_languages",
                columns: new[] { "ProjectId", "Bcp47Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_languages_ProjectId_IsActive",
                table: "project_languages",
                columns: new[] { "ProjectId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_item_language_tasks");

            migrationBuilder.DropTable(
                name: "project_languages");
        }
    }
}

