using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Story44And45ExternalLinksAndActivityFeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "activity_feed_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActorEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false, defaultValue: ""),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_feed_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "external_review_links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CommentEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_review_links", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_activity_feed_events_ActorEmail",
                table: "activity_feed_events",
                column: "ActorEmail");

            migrationBuilder.CreateIndex(
                name: "IX_activity_feed_events_EventType",
                table: "activity_feed_events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_activity_feed_events_ProjectId_CreatedUtc",
                table: "activity_feed_events",
                columns: new[] { "ProjectId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_external_review_links_ContentItemId_ExpiresUtc",
                table: "external_review_links",
                columns: new[] { "ContentItemId", "ExpiresUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_external_review_links_Token",
                table: "external_review_links",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activity_feed_events");

            migrationBuilder.DropTable(
                name: "external_review_links");
        }
    }
}
