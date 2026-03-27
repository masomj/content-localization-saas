using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Story43ReviewWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovedByEmail",
                table: "content_items",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedUtc",
                table: "content_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "content_items",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReviewAssigneeEmail",
                table: "content_items",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedByEmail",
                table: "content_items");

            migrationBuilder.DropColumn(
                name: "ApprovedUtc",
                table: "content_items");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "content_items");

            migrationBuilder.DropColumn(
                name: "ReviewAssigneeEmail",
                table: "content_items");
        }
    }
}

