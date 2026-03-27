using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_project_collections_ProjectId_IsRoot",
                table: "project_collections");

            migrationBuilder.CreateIndex(
                name: "IX_project_collections_ProjectId_IsRoot",
                table: "project_collections",
                columns: new[] { "ProjectId", "IsRoot" },
                unique: true,
                filter: "\"IsRoot\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_project_collections_ProjectId_IsRoot",
                table: "project_collections");

            migrationBuilder.CreateIndex(
                name: "IX_project_collections_ProjectId_IsRoot",
                table: "project_collections",
                columns: new[] { "ProjectId", "IsRoot" },
                unique: true,
                filter: "\"is_root\" = true");
        }
    }
}
