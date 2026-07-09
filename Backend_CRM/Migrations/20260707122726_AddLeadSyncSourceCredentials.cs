using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadSyncSourceCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lead_sync_source_credentials",
                columns: table => new
                {
                    source_id = table.Column<int>(type: "integer", nullable: false),
                    pull_api_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    api_key_encrypted = table.Column<string>(type: "text", nullable: true),
                    configured_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    configured_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lead_sync_source_credentials", x => x.source_id);
                    table.ForeignKey(
                        name: "FK_lead_sync_source_credentials_lead_sync_sources_source_id",
                        column: x => x.source_id,
                        principalTable: "lead_sync_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lead_sync_source_credentials_users_configured_by",
                        column: x => x.configured_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_lead_sync_source_credentials_configured_by",
                table: "lead_sync_source_credentials",
                column: "configured_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lead_sync_source_credentials");
        }
    }
}
