using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContentReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "content_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Verdict = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false, defaultValue: ""),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_reviews_content_items_ContentItemId",
                        column: x => x.ContentItemId,
                        principalTable: "content_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_content_reviews_ContentItemId_CreatedUtc",
                table: "content_reviews",
                columns: new[] { "ContentItemId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_content_reviews_ReviewerEmail",
                table: "content_reviews",
                column: "ReviewerEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_reviews");
        }
    }
}

