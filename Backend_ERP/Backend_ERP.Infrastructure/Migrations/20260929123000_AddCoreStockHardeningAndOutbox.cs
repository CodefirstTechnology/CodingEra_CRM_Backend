using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <summary>
    /// Additive Production Migration: AddCoreStockHardeningAndOutbox
    /// 1. Adds outbox_messages table for transactional outbox pattern.
    /// 2. Adds IdempotencyKey & SecondaryQty columns to stock_transactions.
    /// 3. Adds CatchWeightTolerancePercent to raw_materials.
    /// 4. Alters UnitCost precision to NUMERIC(18,6).
    /// 5. Adds concurrent non-blocking indexes for high-throughput concurrency.
    /// </summary>
    public partial class AddCoreStockHardeningAndOutbox : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create Outbox Messages Table
            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    outbox_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    event_type = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_log = table.Column<string>(type: "text", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.outbox_id);
                });

            // 2. Add IdempotencyKey to stock_transactions (Non-nullable with temporary default)
            migrationBuilder.AddColumn<Guid>(
                name: "IdempotencyKey",
                table: "stock_transactions",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            // 3. Add SecondaryQty to stock_transactions
            migrationBuilder.AddColumn<decimal>(
                name: "SecondaryQty",
                table: "stock_transactions",
                type: "numeric(18,4)",
                nullable: true);

            // 4. Add CatchWeightTolerancePercent to raw_materials
            migrationBuilder.AddColumn<decimal>(
                name: "CatchWeightTolerancePercent",
                table: "raw_materials",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 3.00m);

            // 5. Alter UnitCost precision to NUMERIC(18,6)
            migrationBuilder.AlterColumn<decimal>(
                name: "UnitCost",
                table: "stock_transactions",
                type: "numeric(18,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            // 6. Zero-Downtime Safe Raw SQL Indexes (CONCURRENTLY executed outside transaction block)
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS uq_stock_trans_idempotency ON stock_transactions (\"IdempotencyKey\");",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_outbox_unprocessed ON outbox_messages (created_at) WHERE processed_at IS NULL;",
                suppressTransaction: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "outbox_messages");
            migrationBuilder.DropIndex(name: "uq_stock_trans_idempotency", table: "stock_transactions");
            migrationBuilder.DropColumn(name: "IdempotencyKey", table: "stock_transactions");
            migrationBuilder.DropColumn(name: "SecondaryQty", table: "stock_transactions");
            migrationBuilder.DropColumn(name: "CatchWeightTolerancePercent", table: "raw_materials");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitCost",
                table: "stock_transactions",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)");
        }
    }
}
