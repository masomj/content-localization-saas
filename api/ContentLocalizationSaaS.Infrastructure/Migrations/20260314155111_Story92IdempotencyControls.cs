using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Story92IdempotencyControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "webhook_delivery_logs",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "idempotency_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Operation = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ResponseJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false, defaultValue: ""),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_records", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_webhook_delivery_logs_IdempotencyKey",
                table: "webhook_delivery_logs",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_records_Operation_Key",
                table: "idempotency_records",
                columns: new[] { "Operation", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotency_records");

            migrationBuilder.DropIndex(
                name: "IX_webhook_delivery_logs_IdempotencyKey",
                table: "webhook_delivery_logs");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "webhook_delivery_logs");
        }
    }
}
