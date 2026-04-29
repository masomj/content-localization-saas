using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardenApiTokenScoping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Legacy tokens were not organisation-scoped, so we invalidate them during hardening.
            migrationBuilder.Sql("DELETE FROM api_tokens;");

            migrationBuilder.DropIndex(
                name: "IX_api_tokens_IsRevoked_ExpiresUtc",
                table: "api_tokens");

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "api_tokens",
                type: "uuid",
                nullable: false);

            migrationBuilder.CreateTable(
                name: "api_token_project_scopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiTokenId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_token_project_scopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_api_token_project_scopes_api_tokens_ApiTokenId",
                        column: x => x.ApiTokenId,
                        principalTable: "api_tokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_api_token_project_scopes_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_api_tokens_WorkspaceId_IsRevoked_ExpiresUtc",
                table: "api_tokens",
                columns: new[] { "WorkspaceId", "IsRevoked", "ExpiresUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_api_token_project_scopes_ApiTokenId_ProjectId",
                table: "api_token_project_scopes",
                columns: new[] { "ApiTokenId", "ProjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_api_token_project_scopes_ProjectId",
                table: "api_token_project_scopes",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_api_tokens_workspaces_WorkspaceId",
                table: "api_tokens",
                column: "WorkspaceId",
                principalTable: "workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_api_tokens_workspaces_WorkspaceId",
                table: "api_tokens");

            migrationBuilder.DropTable(
                name: "api_token_project_scopes");

            migrationBuilder.DropIndex(
                name: "IX_api_tokens_WorkspaceId_IsRevoked_ExpiresUtc",
                table: "api_tokens");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "api_tokens");

            migrationBuilder.CreateIndex(
                name: "IX_api_tokens_IsRevoked_ExpiresUtc",
                table: "api_tokens",
                columns: new[] { "IsRevoked", "ExpiresUtc" });
        }
    }
}
