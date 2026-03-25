using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewIdToDiscussionComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReviewId",
                table: "discussion_comments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_discussion_comments_ReviewId",
                table: "discussion_comments",
                column: "ReviewId");

            migrationBuilder.AddForeignKey(
                name: "FK_discussion_comments_content_reviews_ReviewId",
                table: "discussion_comments",
                column: "ReviewId",
                principalTable: "content_reviews",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_discussion_comments_content_reviews_ReviewId",
                table: "discussion_comments");

            migrationBuilder.DropIndex(
                name: "IX_discussion_comments_ReviewId",
                table: "discussion_comments");

            migrationBuilder.DropColumn(
                name: "ReviewId",
                table: "discussion_comments");
        }
    }
}
