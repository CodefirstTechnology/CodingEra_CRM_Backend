using System;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    [DbContext(typeof(ERPDbContext))]
    [Migration("20260929173000_AddGroup2LocationAndTraceability")]
    public partial class AddGroup2LocationAndTraceability : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Warehouse Entities Table
            migrationBuilder.CreateTable(
                name: "warehouse_entities",
                columns: table => new
                {
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    warehouse_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    warehouse_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warehouse_entities", x => x.warehouse_id);
                });

            // 2. Warehouse Zones Table
            migrationBuilder.CreateTable(
                name: "warehouse_zones",
                columns: table => new
                {
                    zone_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    zone_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    zone_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    zone_type = table.Column<string>(type: "text", nullable: false, defaultValue: "Bulk"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warehouse_zones", x => x.zone_id);
                    table.ForeignKey(
                        name: "FK_warehouse_zones_warehouse_entities_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouse_entities",
                        principalColumn: "warehouse_id",
                        onDelete: ReferentialAction.Restrict);
                });

            // 3. Warehouse Bins Table
            migrationBuilder.CreateTable(
                name: "warehouse_bins",
                columns: table => new
                {
                    bin_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    zone_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bin_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    max_weight_kg = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 1000.0000m),
                    max_volume_m3 = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 10.0000m),
                    current_weight_kg = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0.0000m),
                    current_volume_m3 = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0.0000m),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "Active"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warehouse_bins", x => x.bin_id);
                    table.ForeignKey(
                        name: "FK_warehouse_bins_warehouse_zones_zone_id",
                        column: x => x.zone_id,
                        principalTable: "warehouse_zones",
                        principalColumn: "zone_id",
                        onDelete: ReferentialAction.Restrict);
                });

            // 4. Item Batches Table
            migrationBuilder.CreateTable(
                name: "item_batches",
                columns: table => new
                {
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    batch_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    manufacture_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    retest_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    best_before_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    qc_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Released"),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    supplier_lot_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_batches", x => x.batch_id);
                });

            // 5. Additive Columns on stock_transfers table
            migrationBuilder.Sql(@"
                ALTER TABLE stock_transfers ADD COLUMN IF NOT EXISTS ""DispatchDate"" TIMESTAMP WITH TIME ZONE NULL;
                ALTER TABLE stock_transfers ADD COLUMN IF NOT EXISTS ""ReceiptDate"" TIMESTAMP WITH TIME ZONE NULL;
                ALTER TABLE stock_transfers ADD COLUMN IF NOT EXISTS ""EwayBillNumber"" VARCHAR(100) NULL;
                ALTER TABLE stock_transfers ADD COLUMN IF NOT EXISTS ""CarrierName"" VARCHAR(100) NULL;
                ALTER TABLE stock_transfers ADD COLUMN IF NOT EXISTS ""VehicleNumber"" VARCHAR(50) NULL;
                ALTER TABLE stock_transfers ADD COLUMN IF NOT EXISTS ""CreatedByUserId"" UUID NOT NULL DEFAULT gen_random_uuid();
            ");

            // 6. Additive Columns on stock_transfer_items table
            migrationBuilder.Sql(@"
                ALTER TABLE stock_transfer_items ADD COLUMN IF NOT EXISTS ""BatchId"" UUID NOT NULL DEFAULT gen_random_uuid();
                ALTER TABLE stock_transfer_items ADD COLUMN IF NOT EXISTS ""OriginBinId"" UUID NOT NULL DEFAULT gen_random_uuid();
                ALTER TABLE stock_transfer_items ADD COLUMN IF NOT EXISTS ""DestinationBinId"" UUID NULL;
                ALTER TABLE stock_transfer_items ADD COLUMN IF NOT EXISTS ""RequestedQty"" NUMERIC(18,4) NOT NULL DEFAULT 0.0000;
                ALTER TABLE stock_transfer_items ADD COLUMN IF NOT EXISTS ""DispatchedQty"" NUMERIC(18,4) NOT NULL DEFAULT 0.0000;
                ALTER TABLE stock_transfer_items ADD COLUMN IF NOT EXISTS ""ReceivedQty"" NUMERIC(18,4) NOT NULL DEFAULT 0.0000;
                ALTER TABLE stock_transfer_items ADD COLUMN IF NOT EXISTS ""DamagedQty"" NUMERIC(18,4) NOT NULL DEFAULT 0.0000;
                ALTER TABLE stock_transfer_items ADD COLUMN IF NOT EXISTS ""VarianceReason"" VARCHAR(250) NULL;
            ");

            // Non-blocking Concurrent Indexes
            migrationBuilder.Sql("CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS uq_warehouse_entities_code ON warehouse_entities (warehouse_code);", suppressTransaction: true);
            migrationBuilder.Sql("CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS uq_warehouse_bins_code ON warehouse_bins (bin_code);", suppressTransaction: true);
            migrationBuilder.Sql("CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS uq_item_batches_no ON item_batches (item_id, batch_number);", suppressTransaction: true);
            migrationBuilder.Sql("CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_item_batches_expiry ON item_batches (item_id, qc_status, expiry_date ASC) WHERE is_locked = FALSE;", suppressTransaction: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "item_batches");
            migrationBuilder.DropTable(name: "warehouse_bins");
            migrationBuilder.DropTable(name: "warehouse_zones");
            migrationBuilder.DropTable(name: "warehouse_entities");
        }
    }
}
