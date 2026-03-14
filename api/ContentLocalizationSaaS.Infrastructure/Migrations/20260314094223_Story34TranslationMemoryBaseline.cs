using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Story34TranslationMemoryBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TranslationText",
                table: "content_item_language_tasks",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "translation_memory_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: false),
                    TranslationText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_translation_memory_entries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_translation_memory_entries_ProjectId_LanguageCode_SourceTex~",
                table: "translation_memory_entries",
                columns: new[] { "ProjectId", "LanguageCode", "SourceText", "IsApproved" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "translation_memory_entries");

            migrationBuilder.DropColumn(
                name: "TranslationText",
                table: "content_item_language_tasks");
        }
    }
}
