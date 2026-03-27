using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StoryProjectCollectionsTree : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "project_collections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsRoot = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Depth = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_collections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_project_collections_project_collections_ParentId",
                        column: x => x.ParentId,
                        principalTable: "project_collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_collections_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_project_collections_ParentId",
                table: "project_collections",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_project_collections_ProjectId_IsRoot",
                table: "project_collections",
                columns: new[] { "ProjectId", "IsRoot" },
                unique: true,
                filter: "\"IsRoot\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_project_collections_ProjectId_ParentId_Name",
                table: "project_collections",
                columns: new[] { "ProjectId", "ParentId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_collections_ProjectId_ParentId_SortOrder",
                table: "project_collections",
                columns: new[] { "ProjectId", "ParentId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "project_collections");
        }
    }
}

