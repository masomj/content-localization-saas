using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDesignComponents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "design_components",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    FigmaFileId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FigmaFrameId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FigmaFrameName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    ThumbnailUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, defaultValue: ""),
                    FrameWidth = table.Column<int>(type: "integer", nullable: false),
                    FrameHeight = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "draft"),
                    CreatedByEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false, defaultValue: ""),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_design_components", x => x.Id);
                    table.ForeignKey(
                        name: "FK_design_components_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "design_component_text_fields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DesignComponentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FigmaLayerId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FigmaLayerName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    CurrentText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false, defaultValue: ""),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    X = table.Column<double>(type: "double precision", nullable: false),
                    Y = table.Column<double>(type: "double precision", nullable: false),
                    Width = table.Column<double>(type: "double precision", nullable: false),
                    Height = table.Column<double>(type: "double precision", nullable: false),
                    FontFamily = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    FontSize = table.Column<double>(type: "double precision", nullable: false),
                    FontWeight = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: ""),
                    TextAlign = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "left"),
                    Color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: ""),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_design_component_text_fields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_design_component_text_fields_content_items_ContentItemId",
                        column: x => x.ContentItemId,
                        principalTable: "content_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_design_component_text_fields_design_components_DesignCompon~",
                        column: x => x.DesignComponentId,
                        principalTable: "design_components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_design_component_text_fields_ContentItemId",
                table: "design_component_text_fields",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_design_component_text_fields_DesignComponentId",
                table: "design_component_text_fields",
                column: "DesignComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_design_components_ProjectId",
                table: "design_components",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_design_components_ProjectId_FigmaFileId_FigmaFrameId",
                table: "design_components",
                columns: new[] { "ProjectId", "FigmaFileId", "FigmaFrameId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "design_component_text_fields");

            migrationBuilder.DropTable(
                name: "design_components");
        }
    }
}
