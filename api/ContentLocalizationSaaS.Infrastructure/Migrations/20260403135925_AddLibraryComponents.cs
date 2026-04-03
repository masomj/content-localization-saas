using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLibraryComponents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "library_components",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    FigmaFileId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FigmaComponentKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FigmaComponentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    FigmaComponentSetId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, defaultValue: ""),
                    ThumbnailUrl = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    FrameWidth = table.Column<int>(type: "integer", nullable: false),
                    FrameHeight = table.Column<int>(type: "integer", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_library_components", x => x.Id);
                    table.ForeignKey(
                        name: "FK_library_components_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "library_component_variants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LibraryComponentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FigmaNodeId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    VariantName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    VariantProperties = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false, defaultValue: ""),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_library_component_variants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_library_component_variants_library_components_LibraryCompon~",
                        column: x => x.LibraryComponentId,
                        principalTable: "library_components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "library_component_text_fields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LibraryComponentVariantId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_library_component_text_fields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_library_component_text_fields_content_items_ContentItemId",
                        column: x => x.ContentItemId,
                        principalTable: "content_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_library_component_text_fields_library_component_variants_Li~",
                        column: x => x.LibraryComponentVariantId,
                        principalTable: "library_component_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_library_component_text_fields_ContentItemId",
                table: "library_component_text_fields",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_library_component_text_fields_LibraryComponentVariantId",
                table: "library_component_text_fields",
                column: "LibraryComponentVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_library_component_variants_LibraryComponentId",
                table: "library_component_variants",
                column: "LibraryComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_library_component_variants_LibraryComponentId_FigmaNodeId",
                table: "library_component_variants",
                columns: new[] { "LibraryComponentId", "FigmaNodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_library_components_ProjectId",
                table: "library_components",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_library_components_ProjectId_FigmaFileId_FigmaComponentKey",
                table: "library_components",
                columns: new[] { "ProjectId", "FigmaFileId", "FigmaComponentKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "library_component_text_fields");

            migrationBuilder.DropTable(
                name: "library_component_variants");

            migrationBuilder.DropTable(
                name: "library_components");
        }
    }
}
