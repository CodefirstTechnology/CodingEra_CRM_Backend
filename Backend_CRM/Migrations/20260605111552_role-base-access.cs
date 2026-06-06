using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class rolebaseaccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "crm_permissions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    module = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crm_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "crm_role_permissions",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    permission_id = table.Column<int>(type: "integer", nullable: false),
                    access_scope = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crm_role_permissions", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "FK_crm_role_permissions_crm_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "crm_permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_crm_role_permissions_crm_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "crm_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_crm_permissions_code",
                table: "crm_permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_crm_role_permissions_permission_id",
                table: "crm_role_permissions",
                column: "permission_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "crm_role_permissions");

            migrationBuilder.DropTable(
                name: "crm_permissions");
        }
    }
}
