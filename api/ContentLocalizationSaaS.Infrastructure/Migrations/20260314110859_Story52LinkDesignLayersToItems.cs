using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Story52LinkDesignLayersToItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "design_layer_links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    DesignFileId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LayerId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    DuplicateLinkRule = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "preserve"),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_design_layer_links", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_design_layer_links_ContentItemId",
                table: "design_layer_links",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_design_layer_links_ProjectId_DesignFileId_LayerId",
                table: "design_layer_links",
                columns: new[] { "ProjectId", "DesignFileId", "LayerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "design_layer_links");
        }
    }
}

