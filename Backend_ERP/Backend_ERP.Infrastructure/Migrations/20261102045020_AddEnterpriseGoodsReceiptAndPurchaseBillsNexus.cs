using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseGoodsReceiptAndPurchaseBillsNexus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 0. Drop dependent view prior to table/column alterations
            migrationBuilder.Sql("DROP VIEW IF EXISTS mv_vendor_procurement_summary;");
            migrationBuilder.AlterColumn<string>(
                name: "VarianceReason",
                table: "purchase_bills",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TdsSection",
                table: "purchase_bills",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TdsPercentage",
                table: "purchase_bills",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TdsAmount",
                table: "purchase_bills",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "SgstAmount",
                table: "purchase_bills",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "IgstAmount",
                table: "purchase_bills",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "CgstAmount",
                table: "purchase_bills",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<string>(
                name: "ConsolidatedGrnIds",
                table: "purchase_bills",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gstr2BRef",
                table: "purchase_bills",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gstr2BStatus",
                table: "purchase_bills",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMsmeSubjectTo43B",
                table: "purchase_bills",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTds194QApplicable",
                table: "purchase_bills",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsThreeWayMatched",
                table: "purchase_bills",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MatchedAmount",
                table: "purchase_bills",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MsmeAllowedDays",
                table: "purchase_bills",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "MsmeDueDate",
                table: "purchase_bills",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Tds194QAmount",
                table: "purchase_bills",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Tds194QPercentage",
                table: "purchase_bills",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ThreeWayMatchStatus",
                table: "purchase_bills",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "VarianceAmount",
                table: "purchase_bills",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "VarianceOverriddenAt",
                table: "purchase_bills",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VarianceOverriddenBy",
                table: "purchase_bills",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VarianceOverrideReason",
                table: "purchase_bills",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "purchase_bills",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<int>(
                name: "GoodsReceiptId",
                table: "purchase_bill_lines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GrnAcceptedQuantity",
                table: "purchase_bill_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "LineMatchStatus",
                table: "purchase_bill_lines",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MatchRemarks",
                table: "purchase_bill_lines",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PoRate",
                table: "purchase_bill_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityVariance",
                table: "purchase_bill_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RateVariance",
                table: "purchase_bill_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxVariance",
                table: "purchase_bill_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "EWayBillNumber",
                table: "goods_receipts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GateInwardNumber",
                table: "goods_receipts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GateInwardTime",
                table: "goods_receipts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InspectionLotNumber",
                table: "goods_receipts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPostedToInventory",
                table: "goods_receipts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LRNumber",
                table: "goods_receipts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PostedToInventoryAt",
                table: "goods_receipts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QualityStatus",
                table: "goods_receipts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAcceptedAmount",
                table: "goods_receipts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalLandedCost",
                table: "goods_receipts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TransporterName",
                table: "goods_receipts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleNumber",
                table: "goods_receipts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeighbridgeGrossWeight",
                table: "goods_receipts",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeighbridgeNetWeight",
                table: "goods_receipts",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeighbridgeTareWeight",
                table: "goods_receipts",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "goods_receipts",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AlterColumn<string>(
                name: "RejectionReasonCode",
                table: "goods_receipt_items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "BatchNumber",
                table: "goods_receipt_items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BilledQuantity",
                table: "goods_receipt_items",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "goods_receipt_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeatNumber",
                table: "goods_receipt_items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LandedCostPerUnit",
                table: "goods_receipt_items",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MtcNumber",
                table: "goods_receipt_items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PoRate",
                table: "goods_receipt_items",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "QcAcceptedQuantity",
                table: "goods_receipt_items",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "QcDispositionNotes",
                table: "goods_receipt_items",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QcInspectedQuantity",
                table: "goods_receipt_items",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "QcRejectedQuantity",
                table: "goods_receipt_items",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "QcReworkQuantity",
                table: "goods_receipt_items",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValuationAmount",
                table: "goods_receipt_items",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "three_way_match_audit_logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchaseBillId = table.Column<int>(type: "integer", nullable: false),
                    BillNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PurchaseOrderId = table.Column<int>(type: "integer", nullable: true),
                    GoodsReceiptId = table.Column<int>(type: "integer", nullable: true),
                    PurchaseBillLineId = table.Column<int>(type: "integer", nullable: true),
                    ItemName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CheckType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PoValue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    GrnValue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BillValue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Variance = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    VariancePercentage = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    IsWithinTolerance = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DiscrepancyMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OverriddenBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    OverrideReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_three_way_match_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_bills_IsMsmeSubjectTo43B_DueDate_PaymentStatus",
                table: "purchase_bills",
                columns: new[] { "IsMsmeSubjectTo43B", "DueDate", "PaymentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_bills_PurchaseOrderId_GRNId_Status",
                table: "purchase_bills",
                columns: new[] { "PurchaseOrderId", "GRNId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_bills_ThreeWayMatchStatus",
                table: "purchase_bills",
                column: "ThreeWayMatchStatus");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_bills_VendorId",
                table: "purchase_bills",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_bills_VendorId_Status_DueDate",
                table: "purchase_bills",
                columns: new[] { "VendorId", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_bill_lines_GoodsReceiptId",
                table: "purchase_bill_lines",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_bill_lines_GoodsReceiptItemId",
                table: "purchase_bill_lines",
                column: "GoodsReceiptItemId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_GateInwardNumber",
                table: "goods_receipts",
                column: "GateInwardNumber");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_PurchaseOrderId_Status",
                table: "goods_receipts",
                columns: new[] { "PurchaseOrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_items_BatchNumber",
                table: "goods_receipt_items",
                column: "BatchNumber");

            migrationBuilder.CreateIndex(
                name: "IX_three_way_match_audit_logs_BillNumber",
                table: "three_way_match_audit_logs",
                column: "BillNumber");

            migrationBuilder.CreateIndex(
                name: "IX_three_way_match_audit_logs_PurchaseBillId_CheckType_Checked~",
                table: "three_way_match_audit_logs",
                columns: new[] { "PurchaseBillId", "CheckType", "CheckedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_bill_lines_goods_receipt_items_GoodsReceiptItemId",
                table: "purchase_bill_lines",
                column: "GoodsReceiptItemId",
                principalTable: "goods_receipt_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_bill_lines_goods_receipts_GoodsReceiptId",
                table: "purchase_bill_lines",
                column: "GoodsReceiptId",
                principalTable: "goods_receipts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
            // 1. Check constraints
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_grn_weights_positive') THEN
                        ALTER TABLE goods_receipts ADD CONSTRAINT chk_grn_weights_positive 
                        CHECK (""WeighbridgeGrossWeight"" IS NULL OR ""WeighbridgeTareWeight"" IS NULL OR ""WeighbridgeGrossWeight"" >= ""WeighbridgeTareWeight"");
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_grn_item_quantities') THEN
                        ALTER TABLE goods_receipt_items ADD CONSTRAINT chk_grn_item_quantities 
                        CHECK (""ReceivedQuantity"" >= (""AcceptedQuantity"" + ""RejectedQuantity""));
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_bill_amounts') THEN
                        ALTER TABLE purchase_bills ADD CONSTRAINT chk_bill_amounts 
                        CHECK (""GrandTotal"" >= 0 AND ""SubTotal"" >= 0);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_bill_line_qty') THEN
                        ALTER TABLE purchase_bill_lines ADD CONSTRAINT chk_bill_line_qty 
                        CHECK (""Quantity"" > 0 AND ""UnitPrice"" >= 0);
                    END IF;
                END $$;
            ");

            // 2. Triggers: Real-time sync of BilledQuantity on goods_receipt_items
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION fn_trg_sync_grn_item_billed_qty()
                RETURNS TRIGGER AS $$
                DECLARE
                    v_grn_item_id INT;
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        v_grn_item_id := OLD.""GoodsReceiptItemId"";
                    ELSE
                        v_grn_item_id := NEW.""GoodsReceiptItemId"";
                    END IF;

                    IF v_grn_item_id IS NOT NULL AND v_grn_item_id > 0 THEN
                        UPDATE goods_receipt_items
                        SET ""BilledQuantity"" = COALESCE((
                            SELECT SUM(pbl.""Quantity"")
                            FROM purchase_bill_lines pbl
                            JOIN purchase_bills pb ON pb.""Id"" = pbl.""PurchaseBillId""
                            WHERE pbl.""GoodsReceiptItemId"" = v_grn_item_id
                              AND pb.""Status"" NOT IN ('Void', 'Draft')
                              AND pb.""IsDeleted"" = false
                        ), 0.0)
                        WHERE ""Id"" = v_grn_item_id;
                    END IF;

                    IF TG_OP = 'UPDATE' AND OLD.""GoodsReceiptItemId"" IS NOT NULL AND OLD.""GoodsReceiptItemId"" <> NEW.""GoodsReceiptItemId"" THEN
                        UPDATE goods_receipt_items
                        SET ""BilledQuantity"" = COALESCE((
                            SELECT SUM(pbl.""Quantity"")
                            FROM purchase_bill_lines pbl
                            JOIN purchase_bills pb ON pb.""Id"" = pbl.""PurchaseBillId""
                            WHERE pbl.""GoodsReceiptItemId"" = OLD.""GoodsReceiptItemId""
                              AND pb.""Status"" NOT IN ('Void', 'Draft')
                              AND pb.""IsDeleted"" = false
                        ), 0.0)
                        WHERE ""Id"" = OLD.""GoodsReceiptItemId"";
                    END IF;

                    RETURN NULL;
                END;
                $$ LANGUAGE plpgsql;

                DROP TRIGGER IF EXISTS trg_sync_grn_item_billed_qty ON purchase_bill_lines;
                CREATE TRIGGER trg_sync_grn_item_billed_qty
                AFTER INSERT OR UPDATE OF ""Quantity"", ""GoodsReceiptItemId"" OR DELETE ON purchase_bill_lines
                FOR EACH ROW EXECUTE FUNCTION fn_trg_sync_grn_item_billed_qty();
            ");

            // 3. Triggers: Real-time rollup of ReceivedQuantity on purchase_order_lines from GRN posting
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION fn_trg_sync_po_from_grn_posting()
                RETURNS TRIGGER AS $$
                BEGIN
                    IF (NEW.""Status"" = 'Completed' OR NEW.""Status"" = 'Posted') AND (OLD.""Status"" IS DISTINCT FROM NEW.""Status"") THEN
                        UPDATE purchase_order_lines pol
                        SET ""ReceivedQuantity"" = COALESCE((
                            SELECT SUM(gri.""AcceptedQuantity"")
                            FROM goods_receipt_items gri
                            JOIN goods_receipts gr ON gr.""Id"" = gri.""GoodsReceiptId""
                            WHERE gri.""PurchaseOrderLineId"" = pol.""Id""
                              AND gr.""Status"" IN ('Completed', 'Posted')
                              AND gr.""IsDeleted"" = false
                        ), 0.0)
                        WHERE pol.""PurchaseOrderId"" = NEW.""PurchaseOrderId"";
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                DROP TRIGGER IF EXISTS trg_sync_po_from_grn_posting ON goods_receipts;
                CREATE TRIGGER trg_sync_po_from_grn_posting
                AFTER UPDATE OF ""Status"" ON goods_receipts
                FOR EACH ROW EXECUTE FUNCTION fn_trg_sync_po_from_grn_posting();
            ");

            // 4. Recreate view mv_vendor_procurement_summary
            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW mv_vendor_procurement_summary AS
                SELECT 
                    po.""VendorName"" AS vendor_name,
                    COUNT(DISTINCT po.""Id"") AS total_purchase_orders,
                    COALESCE(SUM(po.""TotalAmount""), 0.0) AS total_purchase_value,
                    COUNT(DISTINCT CASE WHEN po.""Status"" = 'Completed' THEN po.""Id"" END) AS completed_orders_count,
                    COUNT(DISTINCT CASE WHEN po.""Status"" IN ('Draft', 'Submitted', 'Approved', 'PartiallyReceived') THEN po.""Id"" END) AS open_orders_count,
                    COUNT(DISTINCT grn.""Id"") AS total_grns,
                    COALESCE(AVG(EXTRACT(EPOCH FROM (grn.""ReceiptDate"" - po.""OrderDate""))/86400.0), 0.0) AS avg_lead_time_days
                FROM purchase_orders po
                LEFT JOIN goods_receipts grn ON grn.""PurchaseOrderNumber"" = po.""PurchaseOrderNumber"" AND grn.""IsDeleted"" = false
                WHERE po.""IsDeleted"" = false
                GROUP BY po.""VendorName"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_purchase_bill_lines_goods_receipt_items_GoodsReceiptItemId",
                table: "purchase_bill_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_purchase_bill_lines_goods_receipts_GoodsReceiptId",
                table: "purchase_bill_lines");

            migrationBuilder.DropTable(
                name: "three_way_match_audit_logs");

            migrationBuilder.DropIndex(
                name: "IX_purchase_bills_IsMsmeSubjectTo43B_DueDate_PaymentStatus",
                table: "purchase_bills");

            migrationBuilder.DropIndex(
                name: "IX_purchase_bills_PurchaseOrderId_GRNId_Status",
                table: "purchase_bills");

            migrationBuilder.DropIndex(
                name: "IX_purchase_bills_ThreeWayMatchStatus",
                table: "purchase_bills");

            migrationBuilder.DropIndex(
                name: "IX_purchase_bills_VendorId",
                table: "purchase_bills");

            migrationBuilder.DropIndex(
                name: "IX_purchase_bills_VendorId_Status_DueDate",
                table: "purchase_bills");

            migrationBuilder.DropIndex(
                name: "IX_purchase_bill_lines_GoodsReceiptId",
                table: "purchase_bill_lines");

            migrationBuilder.DropIndex(
                name: "IX_purchase_bill_lines_GoodsReceiptItemId",
                table: "purchase_bill_lines");

            migrationBuilder.DropIndex(
                name: "IX_goods_receipts_GateInwardNumber",
                table: "goods_receipts");

            migrationBuilder.DropIndex(
                name: "IX_goods_receipts_PurchaseOrderId_Status",
                table: "goods_receipts");

            migrationBuilder.DropIndex(
                name: "IX_goods_receipt_items_BatchNumber",
                table: "goods_receipt_items");

            migrationBuilder.DropColumn(
                name: "ConsolidatedGrnIds",
                table: "purchase_bills");

            migrationBuilder.DropColumn(
                name: "Gstr2BRef",
                table: "purchase_bills");

            migrationBuilder.DropColumn(
                name: "Gstr2BStatus",
                table: "purchase_bills");

            migrationBuilder.DropColumn(
                name: "IsMsmeSubjectTo43B",
                table: "purchase_bills");

            migrationBuilder.DropColumn(
                name: "IsTds194QApplicable",
                table: "purchase_bills");

            migrationBuilder.DropColumn(
                name: "IsThreeWayMatched",
                table: "purchase_bills");

            migrationBuilder.DropColumn(
                name: "MatchedAmount",
                table: "purchase_bills");

            migrationBuilder.DropColumn(
                name: "MsmeAllowedDays",
                table: "purchase_bills");

            migrationBuilder.DropColumn(
                name: "MsmeDueDate",
                table: "purchase_bills");

            migrationBuilder.DropColumn(
                name: "Tds194QAmount",
                table: "purchase_bills");

            migrationBuilder.DropColumn(
                name: "Tds194QPercentage",
                table: "purchase_bills");

            migrationBuilder.DropColumn(
                name: "ThreeWayMatchStatus",
                table: "purchase_bills");

            migrationBuilder.DropColumn(
                name: "VarianceAmount",
                table: "purchase_bills");

            migrationBuilder.DropColumn(
                name: "VarianceOverriddenAt",
                table: "purchase_bills");

            migrationBuilder.DropColumn(
                name: "VarianceOverriddenBy",
                table: "purchase_bills");

            migrationBuilder.DropColumn(
                name: "VarianceOverrideReason",
                table: "purchase_bills");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "purchase_bills");

            migrationBuilder.DropColumn(
                name: "GoodsReceiptId",
                table: "purchase_bill_lines");

            migrationBuilder.DropColumn(
                name: "GrnAcceptedQuantity",
                table: "purchase_bill_lines");

            migrationBuilder.DropColumn(
                name: "LineMatchStatus",
                table: "purchase_bill_lines");

            migrationBuilder.DropColumn(
                name: "MatchRemarks",
                table: "purchase_bill_lines");

            migrationBuilder.DropColumn(
                name: "PoRate",
                table: "purchase_bill_lines");

            migrationBuilder.DropColumn(
                name: "QuantityVariance",
                table: "purchase_bill_lines");

            migrationBuilder.DropColumn(
                name: "RateVariance",
                table: "purchase_bill_lines");

            migrationBuilder.DropColumn(
                name: "TaxVariance",
                table: "purchase_bill_lines");

            migrationBuilder.DropColumn(
                name: "EWayBillNumber",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "GateInwardNumber",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "GateInwardTime",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "InspectionLotNumber",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "IsPostedToInventory",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "LRNumber",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "PostedToInventoryAt",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "QualityStatus",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "TotalAcceptedAmount",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "TotalLandedCost",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "TransporterName",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "VehicleNumber",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "WeighbridgeGrossWeight",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "WeighbridgeNetWeight",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "WeighbridgeTareWeight",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "BatchNumber",
                table: "goods_receipt_items");

            migrationBuilder.DropColumn(
                name: "BilledQuantity",
                table: "goods_receipt_items");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "goods_receipt_items");

            migrationBuilder.DropColumn(
                name: "HeatNumber",
                table: "goods_receipt_items");

            migrationBuilder.DropColumn(
                name: "LandedCostPerUnit",
                table: "goods_receipt_items");

            migrationBuilder.DropColumn(
                name: "MtcNumber",
                table: "goods_receipt_items");

            migrationBuilder.DropColumn(
                name: "PoRate",
                table: "goods_receipt_items");

            migrationBuilder.DropColumn(
                name: "QcAcceptedQuantity",
                table: "goods_receipt_items");

            migrationBuilder.DropColumn(
                name: "QcDispositionNotes",
                table: "goods_receipt_items");

            migrationBuilder.DropColumn(
                name: "QcInspectedQuantity",
                table: "goods_receipt_items");

            migrationBuilder.DropColumn(
                name: "QcRejectedQuantity",
                table: "goods_receipt_items");

            migrationBuilder.DropColumn(
                name: "QcReworkQuantity",
                table: "goods_receipt_items");

            migrationBuilder.DropColumn(
                name: "ValuationAmount",
                table: "goods_receipt_items");

            migrationBuilder.AlterColumn<string>(
                name: "VarianceReason",
                table: "purchase_bills",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TdsSection",
                table: "purchase_bills",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TdsPercentage",
                table: "purchase_bills",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TdsAmount",
                table: "purchase_bills",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "SgstAmount",
                table: "purchase_bills",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "IgstAmount",
                table: "purchase_bills",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "CgstAmount",
                table: "purchase_bills",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<string>(
                name: "RejectionReasonCode",
                table: "goods_receipt_items",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);
        }
    }
}
