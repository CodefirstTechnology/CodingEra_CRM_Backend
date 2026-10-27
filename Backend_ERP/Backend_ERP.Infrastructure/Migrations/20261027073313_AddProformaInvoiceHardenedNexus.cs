using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProformaInvoiceHardenedNexus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalToken",
                table: "proforma_invoices",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "proforma_invoices",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedDate",
                table: "proforma_invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseCurrencyEquivalentAmount",
                table: "proforma_invoices",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CustomerCategory",
                table: "proforma_invoices",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Standard");

            migrationBuilder.AddColumn<string>(
                name: "CustomerGstin",
                table: "proforma_invoices",
                type: "character varying(15)",
                maxLength: 15,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DispatchReleasedBy",
                table: "proforma_invoices",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DispatchReleasedOn",
                table: "proforma_invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ExchangeRateDate",
                table: "proforma_invoices",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Incoterm",
                table: "proforma_invoices",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "EXW");

            migrationBuilder.AddColumn<bool>(
                name: "IsDispatchReleased",
                table: "proforma_invoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PlaceOfSupplyStateCode",
                table: "proforma_invoices",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "27");

            migrationBuilder.AddColumn<string>(
                name: "ProductionReleasedBy",
                table: "proforma_invoices",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "proforma_invoices",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "default");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "proforma_invoices",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "proforma_invoice_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FloorPriceSnapshot",
                table: "proforma_invoice_items",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "HsnCode",
                table: "proforma_invoice_items",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ItemCode",
                table: "proforma_invoice_items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "MaxDiscountPctSnapshot",
                table: "proforma_invoice_items",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalSellingRate",
                table: "proforma_invoice_items",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PlaceOfSupplyStateCode",
                table: "proforma_invoice_items",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "27");

            migrationBuilder.AddColumn<int>(
                name: "QuotationItemId",
                table: "proforma_invoice_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SalesOrderItemId",
                table: "proforma_invoice_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxSlabPercentSnapshot",
                table: "proforma_invoice_items",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalToken",
                table: "proforma_invoice_approval_histories",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApproverUserId",
                table: "proforma_invoice_approval_histories",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "proforma_invoice_approval_histories",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "proforma_invoice_settlement_allocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "default"),
                    ProformaInvoiceId = table.Column<int>(type: "integer", nullable: false),
                    AdvancePaymentId = table.Column<int>(type: "integer", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ExchangeRateAtSettlement = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 1.0000m),
                    BaseCurrencyAllocatedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ReceiptVoucherNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AllocatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AllocatedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsReversed = table.Column<bool>(type: "boolean", nullable: false),
                    ReversedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proforma_invoice_settlement_allocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_proforma_invoice_settlement_allocations_advance_payments_Ad~",
                        column: x => x.AdvancePaymentId,
                        principalTable: "advance_payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_proforma_invoice_settlement_allocations_proforma_invoices_P~",
                        column: x => x.ProformaInvoiceId,
                        principalTable: "proforma_invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_proforma_invoices_TenantId_CustomerId_PaymentStatus",
                table: "proforma_invoices",
                columns: new[] { "TenantId", "CustomerId", "PaymentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_proforma_invoices_TenantId_PiNumber",
                table: "proforma_invoices",
                columns: new[] { "TenantId", "PiNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_proforma_invoices_TenantId_Status_InvoiceDate",
                table: "proforma_invoices",
                columns: new[] { "TenantId", "Status", "InvoiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_proforma_invoice_items_SalesOrderItemId",
                table: "proforma_invoice_items",
                column: "SalesOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_proforma_invoice_settlement_allocations_AdvancePaymentId",
                table: "proforma_invoice_settlement_allocations",
                column: "AdvancePaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_proforma_invoice_settlement_allocations_ProformaInvoiceId_A~",
                table: "proforma_invoice_settlement_allocations",
                columns: new[] { "ProformaInvoiceId", "AdvancePaymentId" });

            migrationBuilder.CreateIndex(
                name: "IX_proforma_invoice_settlement_allocations_TenantId_AllocatedOn",
                table: "proforma_invoice_settlement_allocations",
                columns: new[] { "TenantId", "AllocatedOn" });

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_pi_item_quantity_positive') THEN
                        ALTER TABLE proforma_invoice_items ADD CONSTRAINT chk_pi_item_quantity_positive CHECK (""Quantity"" > 0);
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_pi_milestone_percent') THEN
                        ALTER TABLE proforma_invoices ADD CONSTRAINT chk_pi_milestone_percent 
                        CHECK (""MilestonePercentage"" IS NULL OR (""MilestonePercentage"" > 0 AND ""MilestonePercentage"" <= 100));
                    END IF;
                END $$;

                CREATE OR REPLACE FUNCTION fn_trg_sync_so_proforma_invoiced_totals()
                RETURNS TRIGGER AS $$
                DECLARE
                    v_sales_order_id INT;
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        SELECT ""SalesOrderId"" INTO v_sales_order_id FROM proforma_invoices WHERE ""Id"" = OLD.""ProformaInvoiceId"";
                    ELSE
                        SELECT ""SalesOrderId"" INTO v_sales_order_id FROM proforma_invoices WHERE ""Id"" = NEW.""ProformaInvoiceId"";
                    END IF;

                    IF v_sales_order_id IS NOT NULL THEN
                        UPDATE sales_orders
                        SET ""ProformaInvoicedAmount"" = COALESCE((
                            SELECT SUM(pi.""GrandTotal"")
                            FROM proforma_invoices pi
                            WHERE pi.""SalesOrderId"" = v_sales_order_id
                              AND NOT pi.""IsDeleted""
                              AND pi.""Status"" NOT IN ('Cancelled', 'Rejected', 'Superseded')
                        ), 0.00)
                        WHERE ""Id"" = v_sales_order_id;

                        IF TG_OP = 'INSERT' OR TG_OP = 'UPDATE' THEN
                            IF NEW.""SalesOrderItemId"" IS NOT NULL THEN
                                UPDATE sales_order_items
                                SET ""ProformaInvoicedQuantity"" = COALESCE((
                                    SELECT SUM(pii.""Quantity"")
                                    FROM proforma_invoice_items pii
                                    JOIN proforma_invoices pi ON pi.""Id"" = pii.""ProformaInvoiceId""
                                    WHERE pii.""SalesOrderItemId"" = NEW.""SalesOrderItemId""
                                      AND NOT pi.""IsDeleted""
                                      AND pi.""Status"" NOT IN ('Cancelled', 'Rejected', 'Superseded')
                                ), 0.0000)
                                WHERE ""Id"" = NEW.""SalesOrderItemId"";
                            END IF;
                        ELSIF TG_OP = 'DELETE' THEN
                            IF OLD.""SalesOrderItemId"" IS NOT NULL THEN
                                UPDATE sales_order_items
                                SET ""ProformaInvoicedQuantity"" = COALESCE((
                                    SELECT SUM(pii.""Quantity"")
                                    FROM proforma_invoice_items pii
                                    JOIN proforma_invoices pi ON pi.""Id"" = pii.""ProformaInvoiceId""
                                    WHERE pii.""SalesOrderItemId"" = OLD.""SalesOrderItemId""
                                      AND NOT pi.""IsDeleted""
                                      AND pi.""Status"" NOT IN ('Cancelled', 'Rejected', 'Superseded')
                                ), 0.0000)
                                WHERE ""Id"" = OLD.""SalesOrderItemId"";
                            END IF;
                        END IF;
                    END IF;

                    IF TG_OP = 'DELETE' THEN RETURN OLD; ELSE RETURN NEW; END IF;
                END;
                $$ LANGUAGE plpgsql;

                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_sync_so_proforma_totals') THEN
                        CREATE TRIGGER trg_sync_so_proforma_totals
                        AFTER INSERT OR UPDATE OR DELETE ON proforma_invoice_items
                        FOR EACH ROW EXECUTE FUNCTION fn_trg_sync_so_proforma_invoiced_totals();
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS trg_sync_so_proforma_totals ON proforma_invoice_items;
                DROP FUNCTION IF EXISTS fn_trg_sync_so_proforma_invoiced_totals();
                ALTER TABLE proforma_invoices DROP CONSTRAINT IF EXISTS chk_pi_milestone_percent;
                ALTER TABLE proforma_invoice_items DROP CONSTRAINT IF EXISTS chk_pi_item_quantity_positive;
            ");

            migrationBuilder.DropTable(
                name: "proforma_invoice_settlement_allocations");

            migrationBuilder.DropIndex(
                name: "IX_proforma_invoices_TenantId_CustomerId_PaymentStatus",
                table: "proforma_invoices");

            migrationBuilder.DropIndex(
                name: "IX_proforma_invoices_TenantId_PiNumber",
                table: "proforma_invoices");

            migrationBuilder.DropIndex(
                name: "IX_proforma_invoices_TenantId_Status_InvoiceDate",
                table: "proforma_invoices");

            migrationBuilder.DropIndex(
                name: "IX_proforma_invoice_items_SalesOrderItemId",
                table: "proforma_invoice_items");

            migrationBuilder.DropColumn(
                name: "ApprovalToken",
                table: "proforma_invoices");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "proforma_invoices");

            migrationBuilder.DropColumn(
                name: "ApprovedDate",
                table: "proforma_invoices");

            migrationBuilder.DropColumn(
                name: "BaseCurrencyEquivalentAmount",
                table: "proforma_invoices");

            migrationBuilder.DropColumn(
                name: "CustomerCategory",
                table: "proforma_invoices");

            migrationBuilder.DropColumn(
                name: "CustomerGstin",
                table: "proforma_invoices");

            migrationBuilder.DropColumn(
                name: "DispatchReleasedBy",
                table: "proforma_invoices");

            migrationBuilder.DropColumn(
                name: "DispatchReleasedOn",
                table: "proforma_invoices");

            migrationBuilder.DropColumn(
                name: "ExchangeRateDate",
                table: "proforma_invoices");

            migrationBuilder.DropColumn(
                name: "Incoterm",
                table: "proforma_invoices");

            migrationBuilder.DropColumn(
                name: "IsDispatchReleased",
                table: "proforma_invoices");

            migrationBuilder.DropColumn(
                name: "PlaceOfSupplyStateCode",
                table: "proforma_invoices");

            migrationBuilder.DropColumn(
                name: "ProductionReleasedBy",
                table: "proforma_invoices");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "proforma_invoices");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "proforma_invoices");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "proforma_invoice_items");

            migrationBuilder.DropColumn(
                name: "FloorPriceSnapshot",
                table: "proforma_invoice_items");

            migrationBuilder.DropColumn(
                name: "HsnCode",
                table: "proforma_invoice_items");

            migrationBuilder.DropColumn(
                name: "ItemCode",
                table: "proforma_invoice_items");

            migrationBuilder.DropColumn(
                name: "MaxDiscountPctSnapshot",
                table: "proforma_invoice_items");

            migrationBuilder.DropColumn(
                name: "OriginalSellingRate",
                table: "proforma_invoice_items");

            migrationBuilder.DropColumn(
                name: "PlaceOfSupplyStateCode",
                table: "proforma_invoice_items");

            migrationBuilder.DropColumn(
                name: "QuotationItemId",
                table: "proforma_invoice_items");

            migrationBuilder.DropColumn(
                name: "SalesOrderItemId",
                table: "proforma_invoice_items");

            migrationBuilder.DropColumn(
                name: "TaxSlabPercentSnapshot",
                table: "proforma_invoice_items");

            migrationBuilder.DropColumn(
                name: "ApprovalToken",
                table: "proforma_invoice_approval_histories");

            migrationBuilder.DropColumn(
                name: "ApproverUserId",
                table: "proforma_invoice_approval_histories");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "proforma_invoice_approval_histories");
        }
    }
}
