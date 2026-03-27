using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentLocalizationSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Story14MembershipAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "membership_audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    TargetEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OldValue = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, defaultValue: ""),
                    NewValue = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, defaultValue: ""),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_membership_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_membership_audit_logs_Action",
                table: "membership_audit_logs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_membership_audit_logs_TargetEmail",
                table: "membership_audit_logs",
                column: "TargetEmail");

            migrationBuilder.CreateIndex(
                name: "IX_membership_audit_logs_WorkspaceId_CreatedUtc",
                table: "membership_audit_logs",
                columns: new[] { "WorkspaceId", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "membership_audit_logs");
        }
    }
}

