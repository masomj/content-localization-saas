using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Story51PluginAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "plugin_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UserEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plugin_sessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_plugin_sessions_Token",
                table: "plugin_sessions",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_plugin_sessions_UserEmail_ExpiresUtc",
                table: "plugin_sessions",
                columns: new[] { "UserEmail", "ExpiresUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plugin_sessions");
        }
    }
}

