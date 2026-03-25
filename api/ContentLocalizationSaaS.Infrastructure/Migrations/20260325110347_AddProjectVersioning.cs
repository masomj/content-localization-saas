using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "project_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, defaultValue: ""),
                    Notes = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false, defaultValue: ""),
                    IsLive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false, defaultValue: ""),
                    ContentItemCount = table.Column<int>(type: "integer", nullable: false),
                    TranslationCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_versions_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_version_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    Source = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false, defaultValue: ""),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: ""),
                    Tags = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    CollectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    TranslationsJson = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: false, defaultValue: ""),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_version_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_version_snapshots_project_versions_VersionId",
                        column: x => x.VersionId,
                        principalTable: "project_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_project_version_snapshots_VersionId",
                table: "project_version_snapshots",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_project_version_snapshots_VersionId_Key",
                table: "project_version_snapshots",
                columns: new[] { "VersionId", "Key" });

            migrationBuilder.CreateIndex(
                name: "IX_project_versions_ProjectId_CreatedUtc",
                table: "project_versions",
                columns: new[] { "ProjectId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_project_versions_ProjectId_IsLive",
                table: "project_versions",
                columns: new[] { "ProjectId", "IsLive" },
                unique: true,
                filter: "\"is_live\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_project_versions_ProjectId_Tag",
                table: "project_versions",
                columns: new[] { "ProjectId", "Tag" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "project_version_snapshots");

            migrationBuilder.DropTable(
                name: "project_versions");
        }
    }
}
