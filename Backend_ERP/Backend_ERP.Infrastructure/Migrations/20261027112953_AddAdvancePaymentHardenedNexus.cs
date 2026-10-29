using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancePaymentHardenedNexus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ""IX_advance_payments_PaymentNumber"";
                DROP INDEX IF EXISTS ""IX_advance_payment_receipt_vouchers_VoucherNumber"";
                DROP INDEX IF EXISTS ""IX_advance_payment_applications_AdvancePaymentId_SalesOrderId"";
            ");

            migrationBuilder.AlterColumn<decimal>(
                name: "ExchangeRate",
                table: "advance_payments",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 1.000000m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldPrecision: 18,
                oldScale: 6);

            migrationBuilder.AddColumn<string>(
                name: "PlaceOfSupplyStateCode",
                table: "advance_payments",
                type: "character varying(4)",
                maxLength: 4,
                nullable: false,
                defaultValue: "27");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "advance_payments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "default");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "advance_payments",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdjusted",
                table: "advance_payment_receipt_vouchers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PlaceOfSupplyStateCode",
                table: "advance_payment_receipt_vouchers",
                type: "character varying(4)",
                maxLength: 4,
                nullable: false,
                defaultValue: "27");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "advance_payment_receipt_vouchers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "default");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "advance_payment_applications",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "default");

            migrationBuilder.CreateTable(
                name: "advance_payment_adjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "default"),
                    AdvancePaymentId = table.Column<int>(type: "integer", nullable: false),
                    SalesInvoiceId = table.Column<int>(type: "integer", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AdjustedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AdjustmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExchangeRateAtAdjustment = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 1.0000m),
                    RealizedFxGainLoss = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                    Gstr1VoucherRef = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_advance_payment_adjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_advance_payment_adjustments_advance_payments_AdvancePayment~",
                        column: x => x.AdvancePaymentId,
                        principalTable: "advance_payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_adv_payments_reconciliation_scan",
                table: "advance_payments",
                columns: new[] { "TenantId", "ReconciliationStatus", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "ix_adv_payments_tenant_cust_status",
                table: "advance_payments",
                columns: new[] { "TenantId", "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_adv_payments_tenant_so_status",
                table: "advance_payments",
                columns: new[] { "TenantId", "SalesOrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_adv_payments_unadjusted_float",
                table: "advance_payments",
                columns: new[] { "TenantId", "RemainingAmount" },
                filter: "\"RemainingAmount\" > 0 AND NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_advance_payments_TenantId_PaymentNumber",
                table: "advance_payments",
                columns: new[] { "TenantId", "PaymentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_advance_payment_receipt_vouchers_TenantId_VoucherNumber",
                table: "advance_payment_receipt_vouchers",
                columns: new[] { "TenantId", "VoucherNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_advance_payment_applications_AdvancePaymentId",
                table: "advance_payment_applications",
                column: "AdvancePaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_advance_payment_applications_TenantId_AdvancePaymentId_Sale~",
                table: "advance_payment_applications",
                columns: new[] { "TenantId", "AdvancePaymentId", "SalesOrderId" });

            migrationBuilder.CreateIndex(
                name: "ix_adv_adj_payment_invoice",
                table: "advance_payment_adjustments",
                columns: new[] { "TenantId", "AdvancePaymentId", "SalesInvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_advance_payment_adjustments_AdvancePaymentId",
                table: "advance_payment_adjustments",
                column: "AdvancePaymentId");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_adv_amount_positive') THEN
                        ALTER TABLE advance_payments ADD CONSTRAINT chk_adv_amount_positive CHECK (""AdvanceAmount"" > 0);
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_adv_balance_integrity') THEN
                        ALTER TABLE advance_payments ADD CONSTRAINT chk_adv_balance_integrity 
                        CHECK (""RemainingAmount"" >= 0 AND (""AppliedAmount"" + ""RemainingAmount"" + ""RefundedAmount"" + ""ForfeitedAmount"") <= ""AdvanceAmount"" + 0.05);
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_adv_adj_amount_positive') THEN
                        ALTER TABLE advance_payment_adjustments ADD CONSTRAINT chk_adv_adj_amount_positive CHECK (""AdjustedAmount"" > 0);
                    END IF;
                END $$;

                CREATE OR REPLACE FUNCTION fn_trg_sync_so_advance_allocated_totals()
                RETURNS TRIGGER AS $$
                DECLARE
                    v_sales_order_id INT;
                    v_total_allocated NUMERIC(18,2);
                    v_so_grand_total NUMERIC(18,2);
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        v_sales_order_id := OLD.""SalesOrderId"";
                    ELSE
                        v_sales_order_id := NEW.""SalesOrderId"";
                    END IF;

                    IF v_sales_order_id IS NOT NULL THEN
                        SELECT COALESCE(SUM(
                            CASE 
                                WHEN apa.""IsReversal"" THEN -apa.""ApplyAmount""
                                ELSE apa.""ApplyAmount""
                            END
                        ), 0.00)
                        INTO v_total_allocated
                        FROM advance_payment_applications apa
                        JOIN advance_payments ap ON ap.""Id"" = apa.""AdvancePaymentId""
                        WHERE apa.""SalesOrderId"" = v_sales_order_id
                          AND NOT ap.""IsDeleted""
                          AND ap.""Status"" NOT IN ('Dishonored', 'Cancelled', 'Refunded');

                        IF v_total_allocated < 0 THEN
                            v_total_allocated := 0.00;
                        END IF;

                        SELECT COALESCE(""GrandTotal"", 0.00)
                        INTO v_so_grand_total
                        FROM sales_orders
                        WHERE ""Id"" = v_sales_order_id;

                        UPDATE sales_orders
                        SET ""AdvanceAllocatedAmount"" = v_total_allocated,
                            ""IsProductionReleased"" = CASE
                                WHEN v_so_grand_total > 0 AND (v_total_allocated / v_so_grand_total) >= 0.30 THEN TRUE
                                WHEN v_so_grand_total > 0 AND (v_total_allocated / v_so_grand_total) < 0.30 THEN FALSE
                                ELSE ""IsProductionReleased""
                            END,
                            ""IsDispatchReleased"" = CASE
                                WHEN v_so_grand_total > 0 AND v_total_allocated >= v_so_grand_total THEN TRUE
                                WHEN v_so_grand_total > 0 AND v_total_allocated < v_so_grand_total THEN FALSE
                                ELSE ""IsDispatchReleased""
                            END
                        WHERE ""Id"" = v_sales_order_id;
                    END IF;

                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                DROP TRIGGER IF EXISTS trg_sync_so_advance_allocated_totals ON advance_payment_applications;
                CREATE TRIGGER trg_sync_so_advance_allocated_totals
                AFTER INSERT OR UPDATE OR DELETE ON advance_payment_applications
                FOR EACH ROW
                EXECUTE FUNCTION fn_trg_sync_so_advance_allocated_totals();
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS trg_sync_so_advance_allocated_totals ON advance_payment_applications;
                DROP FUNCTION IF EXISTS fn_trg_sync_so_advance_allocated_totals();

                ALTER TABLE advance_payments DROP CONSTRAINT IF EXISTS chk_adv_amount_positive;
                ALTER TABLE advance_payments DROP CONSTRAINT IF EXISTS chk_adv_balance_integrity;
                ALTER TABLE advance_payment_adjustments DROP CONSTRAINT IF EXISTS chk_adv_adj_amount_positive;
            ");

            migrationBuilder.DropTable(
                name: "advance_payment_adjustments");

            migrationBuilder.DropIndex(
                name: "ix_adv_payments_reconciliation_scan",
                table: "advance_payments");

            migrationBuilder.DropIndex(
                name: "ix_adv_payments_tenant_cust_status",
                table: "advance_payments");

            migrationBuilder.DropIndex(
                name: "ix_adv_payments_tenant_so_status",
                table: "advance_payments");

            migrationBuilder.DropIndex(
                name: "ix_adv_payments_unadjusted_float",
                table: "advance_payments");

            migrationBuilder.DropIndex(
                name: "IX_advance_payments_TenantId_PaymentNumber",
                table: "advance_payments");

            migrationBuilder.DropIndex(
                name: "IX_advance_payment_receipt_vouchers_TenantId_VoucherNumber",
                table: "advance_payment_receipt_vouchers");

            migrationBuilder.DropIndex(
                name: "IX_advance_payment_applications_AdvancePaymentId",
                table: "advance_payment_applications");

            migrationBuilder.DropIndex(
                name: "IX_advance_payment_applications_TenantId_AdvancePaymentId_Sale~",
                table: "advance_payment_applications");

            migrationBuilder.DropColumn(
                name: "PlaceOfSupplyStateCode",
                table: "advance_payments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "advance_payments");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "advance_payments");

            migrationBuilder.DropColumn(
                name: "IsAdjusted",
                table: "advance_payment_receipt_vouchers");

            migrationBuilder.DropColumn(
                name: "PlaceOfSupplyStateCode",
                table: "advance_payment_receipt_vouchers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "advance_payment_receipt_vouchers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "advance_payment_applications");

            migrationBuilder.AlterColumn<decimal>(
                name: "ExchangeRate",
                table: "advance_payments",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldPrecision: 18,
                oldScale: 6,
                oldDefaultValue: 1.000000m);

            migrationBuilder.CreateIndex(
                name: "IX_advance_payments_PaymentNumber",
                table: "advance_payments",
                column: "PaymentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_advance_payment_receipt_vouchers_VoucherNumber",
                table: "advance_payment_receipt_vouchers",
                column: "VoucherNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_advance_payment_applications_AdvancePaymentId_SalesOrderId",
                table: "advance_payment_applications",
                columns: new[] { "AdvancePaymentId", "SalesOrderId" });
        }
    }
}
