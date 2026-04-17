using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStyleRulesAndForbiddenCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresReview",
                table: "content_item_language_tasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "style_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RuleType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Pattern = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    Scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: ""),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_style_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_style_rules_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "style_overrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemLanguageTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    StyleRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverriddenByEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_style_overrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_style_overrides_content_item_language_tasks_ContentItemLang~",
                        column: x => x.ContentItemLanguageTaskId,
                        principalTable: "content_item_language_tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_style_overrides_style_rules_StyleRuleId",
                        column: x => x.StyleRuleId,
                        principalTable: "style_rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_content_item_language_tasks_RequiresReview",
                table: "content_item_language_tasks",
                column: "RequiresReview");

            migrationBuilder.CreateIndex(
                name: "IX_style_overrides_ContentItemLanguageTaskId_StyleRuleId",
                table: "style_overrides",
                columns: new[] { "ContentItemLanguageTaskId", "StyleRuleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_style_overrides_StyleRuleId",
                table: "style_overrides",
                column: "StyleRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_style_rules_ProjectId",
                table: "style_rules",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "style_overrides");

            migrationBuilder.DropTable(
                name: "style_rules");

            migrationBuilder.DropIndex(
                name: "IX_content_item_language_tasks_RequiresReview",
                table: "content_item_language_tasks");

            migrationBuilder.DropColumn(
                name: "RequiresReview",
                table: "content_item_language_tasks");
        }
    }
}
