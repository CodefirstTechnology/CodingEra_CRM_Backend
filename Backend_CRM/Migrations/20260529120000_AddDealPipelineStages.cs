using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_CRM.Migrations
{
    /// <inheritdoc />
    public partial class AddDealPipelineStages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "next_follow_up_date",
                table: "deals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "deal_stage_histories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    deal_id = table.Column<int>(type: "integer", nullable: false),
                    previous_stage = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    new_stage = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    changed_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deal_stage_histories", x => x.id);
                    table.ForeignKey(
                        name: "FK_deal_stage_histories_deals_deal_id",
                        column: x => x.deal_id,
                        principalTable: "deals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_deal_stage_histories_users_changed_by_user_id",
                        column: x => x.changed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_deal_stage_histories_changed_by_user_id",
                table: "deal_stage_histories",
                column: "changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_deal_stage_histories_deal_id_changed_at",
                table: "deal_stage_histories",
                columns: new[] { "deal_id", "changed_at" });

            migrationBuilder.Sql(
                """
                INSERT INTO deal_statuses (name, description, is_active, created_at, updated_at, last_modified)
                VALUES
                  ('Quotation Shared', 'Quotation shared with customer', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                  ('Follow-Up Ongoing', 'Active follow-up in progress', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                  ('Site Visit / Meeting Done', 'Site visit or meeting completed', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                  ('Technical Approval', 'Technical approval stage', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                  ('Sample Approval', 'Sample approval stage', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                  ('Negotiation Stage', 'Deal under negotiation', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                  ('PO Received', 'Purchase order received', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                  ('Advance Payment Pending', 'Awaiting advance payment', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                  ('Advance Payment Received', 'Advance payment received', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                  ('Production Started', 'Production has started', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                  ('Material Ready For Dispatch', 'Material ready for dispatch', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                  ('Full Payment Pending', 'Awaiting full payment', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                  ('Full Payment Received', 'Full payment received', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                  ('Material Dispatched', 'Material dispatched', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                  ('Material Delivered', 'Material delivered', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                  ('Lead Closed - Won', 'Deal closed as won', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc'),
                  ('Lead Closed - Lost', 'Deal closed as lost', true, NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc', NOW() AT TIME ZONE 'utc')
                ON CONFLICT (name) DO NOTHING;

                UPDATE deals d
                SET deal_status_id = ds.id, status = ds.name
                FROM deal_statuses ds
                WHERE LOWER(TRIM(d.status)) = 'qualification'
                  AND ds.name = 'Quotation Shared'
                  AND ds.is_active = true;

                UPDATE deals d
                SET deal_status_id = ds.id, status = ds.name
                FROM deal_statuses ds
                WHERE LOWER(TRIM(d.status)) = 'proposal'
                  AND ds.name = 'Quotation Shared'
                  AND ds.is_active = true;

                UPDATE deals d
                SET deal_status_id = ds.id, status = ds.name
                FROM deal_statuses ds
                WHERE LOWER(TRIM(d.status)) = 'negotiation'
                  AND ds.name = 'Negotiation Stage'
                  AND ds.is_active = true;

                UPDATE deals d
                SET deal_status_id = ds.id, status = ds.name
                FROM deal_statuses ds
                WHERE LOWER(TRIM(d.status)) IN ('demo/making', 'demo')
                  AND ds.name = 'Technical Approval'
                  AND ds.is_active = true;

                UPDATE deals d
                SET deal_status_id = ds.id, status = ds.name
                FROM deal_statuses ds
                WHERE LOWER(TRIM(d.status)) = 'closed won'
                  AND ds.name = 'Lead Closed - Won'
                  AND ds.is_active = true;

                UPDATE deals d
                SET deal_status_id = ds.id, status = ds.name
                FROM deal_statuses ds
                WHERE LOWER(TRIM(d.status)) = 'closed lost'
                  AND ds.name = 'Lead Closed - Lost'
                  AND ds.is_active = true;

                UPDATE deal_statuses
                SET is_active = false,
                    updated_at = NOW() AT TIME ZONE 'utc',
                    last_modified = NOW() AT TIME ZONE 'utc'
                WHERE name IN (
                  'Qualification', 'Proposal', 'Negotiation', 'Demo/Making', 'Closed Won', 'Closed Lost'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE deal_statuses SET is_active = true,
                    updated_at = NOW() AT TIME ZONE 'utc',
                    last_modified = NOW() AT TIME ZONE 'utc'
                WHERE name IN (
                  'Qualification', 'Proposal', 'Negotiation', 'Demo/Making', 'Closed Won', 'Closed Lost'
                );
                """);

            migrationBuilder.DropTable(
                name: "deal_stage_histories");

            migrationBuilder.DropColumn(
                name: "next_follow_up_date",
                table: "deals");
        }
    }
}
