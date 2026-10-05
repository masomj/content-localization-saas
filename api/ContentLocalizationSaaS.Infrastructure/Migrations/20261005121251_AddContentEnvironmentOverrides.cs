using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContentEnvironmentOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "content_environment_overrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Environment = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: false, defaultValue: ""),
                    Value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    UpdatedByEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false, defaultValue: ""),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_environment_overrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_environment_overrides_content_items_ContentItemId",
                        column: x => x.ContentItemId,
                        principalTable: "content_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_content_environment_overrides_ContentItemId_Environment_Lan~",
                table: "content_environment_overrides",
                columns: new[] { "ContentItemId", "Environment", "LanguageCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_content_environment_overrides_ProjectId_Environment",
                table: "content_environment_overrides",
                columns: new[] { "ProjectId", "Environment" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_environment_overrides");
        }
    }
}
