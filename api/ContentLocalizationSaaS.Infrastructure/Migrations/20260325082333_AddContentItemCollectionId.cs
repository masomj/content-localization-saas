using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContentItemCollectionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CollectionId",
                table: "content_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "content_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_content_items_CollectionId",
                table: "content_items",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_content_items_ProjectId_CollectionId",
                table: "content_items",
                columns: new[] { "ProjectId", "CollectionId" });

            migrationBuilder.AddForeignKey(
                name: "FK_content_items_project_collections_CollectionId",
                table: "content_items",
                column: "CollectionId",
                principalTable: "project_collections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_content_items_project_collections_CollectionId",
                table: "content_items");

            migrationBuilder.DropIndex(
                name: "IX_content_items_CollectionId",
                table: "content_items");

            migrationBuilder.DropIndex(
                name: "IX_content_items_ProjectId_CollectionId",
                table: "content_items");

            migrationBuilder.DropColumn(
                name: "CollectionId",
                table: "content_items");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "content_items");
        }
    }
}
