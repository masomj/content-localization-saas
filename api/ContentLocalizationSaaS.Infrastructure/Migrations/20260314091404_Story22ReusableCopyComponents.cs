using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Story22ReusableCopyComponents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CopyComponentId",
                table: "content_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "copy_components",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Source = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_copy_components", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_content_items_CopyComponentId",
                table: "content_items",
                column: "CopyComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_copy_components_ProjectId_Name",
                table: "copy_components",
                columns: new[] { "ProjectId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "copy_components");

            migrationBuilder.DropIndex(
                name: "IX_content_items_CopyComponentId",
                table: "content_items");

            migrationBuilder.DropColumn(
                name: "CopyComponentId",
                table: "content_items");
        }
    }
}

