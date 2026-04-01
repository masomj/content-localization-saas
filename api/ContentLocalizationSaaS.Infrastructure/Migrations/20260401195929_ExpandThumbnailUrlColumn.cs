using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandThumbnailUrlColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ThumbnailUrl",
                table: "design_components",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldDefaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ThumbnailUrl",
                table: "design_components",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "");
        }
    }
}
