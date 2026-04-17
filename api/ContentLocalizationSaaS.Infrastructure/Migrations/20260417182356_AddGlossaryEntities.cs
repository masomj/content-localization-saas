using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGlossaryEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "glossaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_glossaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_glossaries_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "glossary_terms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GlossaryId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceTerm = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Definition = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    IsForbidden = table.Column<bool>(type: "boolean", nullable: false),
                    ForbiddenReplacement = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    CaseSensitive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_glossary_terms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_glossary_terms_glossaries_GlossaryId",
                        column: x => x.GlossaryId,
                        principalTable: "glossaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "glossary_term_translations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GlossaryTermId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TranslatedTerm = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_glossary_term_translations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_glossary_term_translations_glossary_terms_GlossaryTermId",
                        column: x => x.GlossaryTermId,
                        principalTable: "glossary_terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_glossaries_WorkspaceId",
                table: "glossaries",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_glossary_term_translations_GlossaryTermId_LanguageCode",
                table: "glossary_term_translations",
                columns: new[] { "GlossaryTermId", "LanguageCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_glossary_terms_GlossaryId",
                table: "glossary_terms",
                column: "GlossaryId");

            migrationBuilder.CreateIndex(
                name: "IX_glossary_terms_SourceTerm",
                table: "glossary_terms",
                column: "SourceTerm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "glossary_term_translations");

            migrationBuilder.DropTable(
                name: "glossary_terms");

            migrationBuilder.DropTable(
                name: "glossaries");
        }
    }
}
