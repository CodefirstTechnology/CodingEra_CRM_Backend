using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    [DbContext(typeof(ERPDbContext))]
    [Migration("20260921150000_OptimizeProcurementReportsSchema")]
    public partial class OptimizeProcurementReportsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    -- 1. Add VendorId to purchase_orders if missing
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'purchase_orders' AND (column_name = 'VendorId' OR column_name = 'vendor_id')
                    ) THEN
                        ALTER TABLE purchase_orders ADD COLUMN "VendorId" integer NULL;
                    END IF;

                    -- 2. Add line audit columns to purchase_order_lines
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'purchase_order_lines' AND (column_name = 'ItemId' OR column_name = 'item_id')
                    ) THEN
                        ALTER TABLE purchase_order_lines ADD COLUMN "ItemId" integer NULL;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'purchase_order_lines' AND (column_name = 'UomId' OR column_name = 'uom_id')
                    ) THEN
                        ALTER TABLE purchase_order_lines ADD COLUMN "UomId" integer NULL;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'purchase_order_lines' AND (column_name = 'ReceivedQuantity' OR column_name = 'received_quantity')
                    ) THEN
                        ALTER TABLE purchase_order_lines ADD COLUMN "ReceivedQuantity" numeric(18,4) NOT NULL DEFAULT 0.0;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'purchase_order_lines' AND (column_name = 'BilledQuantity' OR column_name = 'billed_quantity')
                    ) THEN
                        ALTER TABLE purchase_order_lines ADD COLUMN "BilledQuantity" numeric(18,4) NOT NULL DEFAULT 0.0;
                    END IF;

                    -- 3. Add VendorId to goods_receipts
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'goods_receipts' AND (column_name = 'VendorId' OR column_name = 'vendor_id')
                    ) THEN
                        ALTER TABLE goods_receipts ADD COLUMN "VendorId" integer NULL;
                    END IF;

                    -- 4. Add ItemId and AcceptedQuantity to goods_receipt_items
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'goods_receipt_items' AND (column_name = 'ItemId' OR column_name = 'item_id')
                    ) THEN
                        ALTER TABLE goods_receipt_items ADD COLUMN "ItemId" integer NULL;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'goods_receipt_items' AND (column_name = 'AcceptedQuantity' OR column_name = 'accepted_quantity')
                    ) THEN
                        ALTER TABLE goods_receipt_items ADD COLUMN "AcceptedQuantity" numeric(18,4) NOT NULL DEFAULT 0.0;
                    END IF;

                    -- 5. Foreign Key Constraints (Idempotent)
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints
                        WHERE constraint_name = 'FK_purchase_orders_vendors_VendorId' OR constraint_name = 'FK_purchase_orders_vendors_vendor_id'
                    ) THEN
                        ALTER TABLE purchase_orders
                        ADD CONSTRAINT "FK_purchase_orders_vendors_VendorId"
                        FOREIGN KEY ("VendorId") REFERENCES vendors ("Id") ON DELETE SET NULL;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints
                        WHERE constraint_name = 'FK_goods_receipts_vendors_VendorId' OR constraint_name = 'FK_goods_receipts_vendors_vendor_id'
                    ) THEN
                        ALTER TABLE goods_receipts
                        ADD CONSTRAINT "FK_goods_receipts_vendors_VendorId"
                        FOREIGN KEY ("VendorId") REFERENCES vendors ("Id") ON DELETE SET NULL;
                    END IF;

                    -- 6. High-Performance Composite Indexes
                    CREATE INDEX IF NOT EXISTS "idx_po_reports_filtering"
                    ON purchase_orders ("IsDeleted", "OrderDate", "Status", "VendorName");

                    CREATE INDEX IF NOT EXISTS "idx_grn_reports_filtering"
                    ON goods_receipts ("IsDeleted", "ReceiptDate", "Status", "VendorName", "PurchaseOrderNumber");

                    CREATE INDEX IF NOT EXISTS "idx_po_lines_po_id"
                    ON purchase_order_lines ("PurchaseOrderId");

                    CREATE INDEX IF NOT EXISTS "idx_grn_items_grn_id"
                    ON goods_receipt_items ("GoodsReceiptId");

                    -- 7. Create database view: mv_vendor_procurement_summary
                    CREATE OR REPLACE VIEW mv_vendor_procurement_summary AS
                    SELECT 
                        po."VendorName" AS vendor_name,
                        COUNT(DISTINCT po."Id") AS total_purchase_orders,
                        COALESCE(SUM(po."TotalAmount"), 0.0) AS total_purchase_value,
                        COUNT(DISTINCT CASE WHEN po."Status" = 'Completed' THEN po."Id" END) AS completed_orders_count,
                        COUNT(DISTINCT CASE WHEN po."Status" IN ('Draft', 'Submitted', 'Approved', 'PartiallyReceived') THEN po."Id" END) AS open_orders_count,
                        COUNT(DISTINCT grn."Id") AS total_grns,
                        COALESCE(AVG(EXTRACT(EPOCH FROM (grn."ReceiptDate" - po."OrderDate"))/86400.0), 0.0) AS avg_lead_time_days
                    FROM purchase_orders po
                    LEFT JOIN goods_receipts grn ON grn."PurchaseOrderNumber" = po."PurchaseOrderNumber" AND grn."IsDeleted" = false
                    WHERE po."IsDeleted" = false
                    GROUP BY po."VendorName";

                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP VIEW IF EXISTS mv_vendor_procurement_summary;
                DROP INDEX IF EXISTS idx_po_reports_filtering;
                DROP INDEX IF EXISTS idx_grn_reports_filtering;
                DROP INDEX IF EXISTS idx_po_lines_po_id;
                DROP INDEX IF EXISTS idx_grn_items_grn_id;
                """);
        }
    }
}
