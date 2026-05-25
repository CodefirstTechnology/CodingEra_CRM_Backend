using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class AddDealStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "deal_status_id",
                table: "deals",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "deal_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deal_statuses", x => x.id);
                    table.ForeignKey(
                        name: "FK_deal_statuses_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_deal_statuses_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_deals_deal_status_id",
                table: "deals",
                column: "deal_status_id");

            migrationBuilder.CreateIndex(
                name: "IX_deal_statuses_created_by",
                table: "deal_statuses",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_deal_statuses_name",
                table: "deal_statuses",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_deal_statuses_updated_by",
                table: "deal_statuses",
                column: "updated_by");

            migrationBuilder.AddForeignKey(
                name: "FK_deals_deal_statuses_deal_status_id",
                table: "deals",
                column: "deal_status_id",
                principalTable: "deal_statuses",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql(
                """
                INSERT INTO deal_statuses (name, description, is_active, created_at, updated_at, last_modified)
                VALUES
                  ('Qualification', 'Initial deal qualification stage', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                  ('Proposal', 'Proposal sent to customer', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                  ('Negotiation', 'Deal under negotiation', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                  ('Demo/Making', 'Demo or solution in progress', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                  ('Closed Won', 'Deal successfully closed', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                  ('Closed Lost', 'Deal lost', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc')
                ON CONFLICT (name) DO NOTHING;

                UPDATE deals d
                SET deal_status_id = ds.id
                FROM deal_statuses ds
                WHERE d.deal_status_id IS NULL
                  AND LOWER(TRIM(d.status)) = LOWER(TRIM(ds.name))
                  AND ds.is_active = true;

                UPDATE deals d
                SET deal_status_id = ds.id,
                    status = ds.name
                FROM deal_statuses ds
                WHERE d.deal_status_id IS NULL
                  AND ds.name = 'Qualification'
                  AND ds.is_active = true;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_deals_deal_statuses_deal_status_id",
                table: "deals");

            migrationBuilder.DropTable(
                name: "deal_statuses");

            migrationBuilder.DropIndex(
                name: "IX_deals_deal_status_id",
                table: "deals");

            migrationBuilder.DropColumn(
                name: "deal_status_id",
                table: "deals");
        }
    }
}
