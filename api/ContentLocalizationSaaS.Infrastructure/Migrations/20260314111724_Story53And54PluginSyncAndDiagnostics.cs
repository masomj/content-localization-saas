using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Story53And54PluginSyncAndDiagnostics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "plugin_sync_conflicts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DesignLayerLinkId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false, defaultValue: ""),
                    ProposedText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false, defaultValue: ""),
                    ResolutionState = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "open"),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plugin_sync_conflicts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_plugin_sync_conflicts_DesignLayerLinkId_ResolutionState_Cre~",
                table: "plugin_sync_conflicts",
                columns: new[] { "DesignLayerLinkId", "ResolutionState", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plugin_sync_conflicts");
        }
    }
}

