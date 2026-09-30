using System;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    [DbContext(typeof(ERPDbContext))]
    [Migration("20260930140000_AddGroup3ControlAndAuditing")]
    public partial class AddGroup3ControlAndAuditing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Stock Alert Thresholds Table
            migrationBuilder.CreateTable(
                name: "stock_alert_thresholds",
                columns: table => new
                {
                    threshold_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    min_stock_level = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    reorder_point = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    max_stock_level = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    safety_stock = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_alert_thresholds", x => x.threshold_id);
                });

            // 2. Stock Alert Logs Table
            migrationBuilder.CreateTable(
                name: "stock_alert_logs",
                columns: table => new
                {
                    alert_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    threshold_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alert_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    alert_priority = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Warning"),
                    current_qty = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    available_qty = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    on_order_qty = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Active"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_alert_logs", x => x.alert_id);
                    table.ForeignKey(
                        name: "FK_stock_alert_logs_stock_alert_thresholds_threshold_id",
                        column: x => x.threshold_id,
                        principalTable: "stock_alert_thresholds",
                        principalColumn: "threshold_id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 3. Item Valuation Layers Table (FIFO)
            migrationBuilder.CreateTable(
                name: "item_valuation_layers",
                columns: table => new
                {
                    layer_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    unit_cost = table.Column<decimal>(type: "numeric(18,6)", nullable: false, defaultValue: 0m),
                    initial_qty = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    remaining_qty = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    is_depleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_valuation_layers", x => x.layer_id);
                });

            // 4. Item WAC Balances Table (Weighted Average Cost)
            migrationBuilder.CreateTable(
                name: "item_wac_balances",
                columns: table => new
                {
                    balance_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_qty = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    total_value = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    weighted_avg_cost = table.Column<decimal>(type: "numeric(18,6)", nullable: false, defaultValue: 0m),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_wac_balances", x => x.balance_id);
                });

            // 5. Physical Count Sheets Table
            migrationBuilder.CreateTable(
                name: "physical_count_sheets",
                columns: table => new
                {
                    sheet_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    sheet_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    count_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Draft"),
                    conducted_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    reconciled_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    reconciled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_physical_count_sheets", x => x.sheet_id);
                });

            // 6. Physical Count Lines Table
            migrationBuilder.CreateTable(
                name: "physical_count_lines",
                columns: table => new
                {
                    line_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    sheet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    bin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    system_qty = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    counted_qty = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    variance_qty = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    variance_reason = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_physical_count_lines", x => x.line_id);
                    table.ForeignKey(
                        name: "FK_physical_count_lines_physical_count_sheets_sheet_id",
                        column: x => x.sheet_id,
                        principalTable: "physical_count_sheets",
                        principalColumn: "sheet_id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Non-blocking Concurrent Indexes
            migrationBuilder.Sql("CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS uq_stock_alert_thresholds_item_wh ON stock_alert_thresholds (item_id, warehouse_id);", suppressTransaction: true);
            migrationBuilder.Sql("CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_item_valuation_layers_fifo ON item_valuation_layers (item_id, warehouse_id, is_depleted, receipt_date ASC);", suppressTransaction: true);
            migrationBuilder.Sql("CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS uq_item_wac_balances_item_wh ON item_wac_balances (item_id, warehouse_id);", suppressTransaction: true);
            migrationBuilder.Sql("CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS uq_physical_count_sheets_no ON physical_count_sheets (sheet_number);", suppressTransaction: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "physical_count_lines");
            migrationBuilder.DropTable(name: "physical_count_sheets");
            migrationBuilder.DropTable(name: "item_wac_balances");
            migrationBuilder.DropTable(name: "item_valuation_layers");
            migrationBuilder.DropTable(name: "stock_alert_logs");
            migrationBuilder.DropTable(name: "stock_alert_thresholds");
        }
    }
}
