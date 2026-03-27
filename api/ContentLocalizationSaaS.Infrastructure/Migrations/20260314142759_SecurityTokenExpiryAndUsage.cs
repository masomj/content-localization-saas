using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SecurityTokenExpiryAndUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresUtc",
                table: "api_tokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUsedUtc",
                table: "api_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_api_tokens_IsRevoked_ExpiresUtc",
                table: "api_tokens",
                columns: new[] { "IsRevoked", "ExpiresUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_api_tokens_IsRevoked_ExpiresUtc",
                table: "api_tokens");

            migrationBuilder.DropColumn(
                name: "ExpiresUtc",
                table: "api_tokens");

            migrationBuilder.DropColumn(
                name: "LastUsedUtc",
                table: "api_tokens");
        }
    }
}

