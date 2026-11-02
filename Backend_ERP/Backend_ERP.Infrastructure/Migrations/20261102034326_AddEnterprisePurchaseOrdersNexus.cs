using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterprisePurchaseOrdersNexus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP VIEW IF EXISTS mv_vendor_procurement_summary;
                ALTER TABLE purchase_orders DROP CONSTRAINT IF EXISTS ""FK_purchase_orders_vendors_VendorId"";
                DROP INDEX IF EXISTS ""IX_purchase_orders_PurchaseOrderNumber"";
                DROP INDEX IF EXISTS ""IX_purchase_orders_Status_IsDeleted_OrderDate"";
                DROP INDEX IF EXISTS ""IX_purchase_orders_VendorId_Status_OrderDate"";
                DROP INDEX IF EXISTS ""IX_purchase_order_lines_UomId"";
            ");
            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "purchase_orders",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "purchase_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxTotal",
                table: "purchase_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "Subtotal",
                table: "purchase_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "ShippingAddress",
                table: "purchase_orders",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "SalesOrderNumber",
                table: "purchase_orders",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Remarks",
                table: "purchase_orders",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "purchase_orders",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "GstNumber",
                table: "purchase_orders",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountTotal",
                table: "purchase_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "purchase_orders",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "BuyerName",
                table: "purchase_orders",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "BillingAddress",
                table: "purchase_orders",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<DateTime>(
                name: "AcknowledgedAt",
                table: "purchase_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AmendmentReason",
                table: "purchase_orders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AmendmentType",
                table: "purchase_orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalToken",
                table: "purchase_orders",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "purchase_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByUserId",
                table: "purchase_orders",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentApprovalTier",
                table: "purchase_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ExceptionReasonsJson",
                table: "purchase_orders",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "purchase_orders",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 1.000000m);

            migrationBuilder.AddColumn<decimal>(
                name: "FreightAmount",
                table: "purchase_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GrandTotalBaseCurrency",
                table: "purchase_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Incoterms",
                table: "purchase_orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "FOR Destination");

            migrationBuilder.AddColumn<decimal>(
                name: "InsuranceAmount",
                table: "purchase_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsCurrentRevision",
                table: "purchase_orders",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsExceptionApproval",
                table: "purchase_orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ParentPOId",
                table: "purchase_orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentTermsDays",
                table: "purchase_orders",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<int>(
                name: "PurchaseRequisitionId",
                table: "purchase_orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RFQId",
                table: "purchase_orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequiredApprovalTier",
                table: "purchase_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RevisionNumber",
                table: "purchase_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SupersededAt",
                table: "purchase_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "purchase_orders",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCgstAmount",
                table: "purchase_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalIgstAmount",
                table: "purchase_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalSgstAmount",
                table: "purchase_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "VendorCode",
                table: "purchase_orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "VendorComparisonId",
                table: "purchase_orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VendorGstin",
                table: "purchase_orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VendorPan",
                table: "purchase_orders",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VendorStateCode",
                table: "purchase_orders",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "purchase_orders",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "purchase_order_lines",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Nos",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldDefaultValue: "Nos");

            migrationBuilder.AlterColumn<decimal>(
                name: "Tax",
                table: "purchase_order_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "purchase_order_lines",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<decimal>(
                name: "BilledQuantity",
                table: "purchase_order_lines",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "purchase_order_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BaseUom",
                table: "purchase_order_lines",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CancelledQuantity",
                table: "purchase_order_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CgstAmount",
                table: "purchase_order_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryScheduleJson",
                table: "purchase_order_lines",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "purchase_order_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "purchase_order_lines",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedDeliveryDate",
                table: "purchase_order_lines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GstRatePercentage",
                table: "purchase_order_lines",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 18m);

            migrationBuilder.AddColumn<string>(
                name: "HsnCode",
                table: "purchase_order_lines",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "IgstAmount",
                table: "purchase_order_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InvoicedQuantity",
                table: "purchase_order_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsItcEligible",
                table: "purchase_order_lines",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemCode",
                table: "purchase_order_lines",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ItemType",
                table: "purchase_order_lines",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "LineNumber",
                table: "purchase_order_lines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LineStatus",
                table: "purchase_order_lines",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "LineTotal",
                table: "purchase_order_lines",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OrderedQuantity",
                table: "purchase_order_lines",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OverDeliveryTolerancePercent",
                table: "purchase_order_lines",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PurchaseRequisitionLineId",
                table: "purchase_order_lines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RFQLineId",
                table: "purchase_order_lines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RejectedQuantity",
                table: "purchase_order_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SgstAmount",
                table: "purchase_order_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TargetWarehouseId",
                table: "purchase_order_lines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "purchase_order_lines",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxableAmount",
                table: "purchase_order_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "purchase_order_lines",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitRate",
                table: "purchase_order_lines",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "VendorQuotationLineId",
                table: "purchase_order_lines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "purchase_order_lines",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "purchase_order_amendment_histories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    PurchaseOrderId = table.Column<int>(type: "integer", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    ChangedField = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OldValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true),
                    AmendmentReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ActionedByUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_order_amendment_histories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchase_order_amendment_histories_purchase_orders_Purchase~",
                        column: x => x.PurchaseOrderId,
                        principalTable: "purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_approvals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    PurchaseOrderId = table.Column<int>(type: "integer", nullable: false),
                    TierLevel = table.Column<int>(type: "integer", nullable: false),
                    ApproverRole = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ApproverUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ApproverName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Decision = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Comments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    HmacSignatureToken = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsExceptionApproval = table.Column<bool>(type: "boolean", nullable: false),
                    ActionedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_order_approvals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchase_order_approvals_purchase_orders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_ParentPOId",
                table: "purchase_orders",
                column: "ParentPOId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_PurchaseRequisitionId",
                table: "purchase_orders",
                column: "PurchaseRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_RFQId",
                table: "purchase_orders",
                column: "RFQId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_TenantId_Status_OrderDate",
                table: "purchase_orders",
                columns: new[] { "TenantId", "Status", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_TenantId_Status_RequiredApprovalTier_Curren~",
                table: "purchase_orders",
                columns: new[] { "TenantId", "Status", "RequiredApprovalTier", "CurrentApprovalTier" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_VendorComparisonId",
                table: "purchase_orders",
                column: "VendorComparisonId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_PurchaseOrderId_LineStatus",
                table: "purchase_order_lines",
                columns: new[] { "PurchaseOrderId", "LineStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_PurchaseRequisitionLineId",
                table: "purchase_order_lines",
                column: "PurchaseRequisitionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_RFQLineId",
                table: "purchase_order_lines",
                column: "RFQLineId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_VendorQuotationLineId",
                table: "purchase_order_lines",
                column: "VendorQuotationLineId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_amendment_histories_PurchaseOrderId_Revision~",
                table: "purchase_order_amendment_histories",
                columns: new[] { "PurchaseOrderId", "RevisionNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_approvals_PurchaseOrderId_TierLevel",
                table: "purchase_order_approvals",
                columns: new[] { "PurchaseOrderId", "TierLevel" });

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_order_lines_purchase_requisition_lines_PurchaseReq~",
                table: "purchase_order_lines",
                column: "PurchaseRequisitionLineId",
                principalTable: "purchase_requisition_lines",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_order_lines_rfq_lines_RFQLineId",
                table: "purchase_order_lines",
                column: "RFQLineId",
                principalTable: "rfq_lines",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_order_lines_vendor_quotation_lines_VendorQuotation~",
                table: "purchase_order_lines",
                column: "VendorQuotationLineId",
                principalTable: "vendor_quotation_lines",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_orders_purchase_orders_ParentPOId",
                table: "purchase_orders",
                column: "ParentPOId",
                principalTable: "purchase_orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_orders_purchase_requisitions_PurchaseRequisitionId",
                table: "purchase_orders",
                column: "PurchaseRequisitionId",
                principalTable: "purchase_requisitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_orders_request_for_quotations_RFQId",
                table: "purchase_orders",
                column: "RFQId",
                principalTable: "request_for_quotations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_orders_vendor_comparisons_VendorComparisonId",
                table: "purchase_orders",
                column: "VendorComparisonId",
                principalTable: "vendor_comparisons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_orders_vendors_VendorId",
                table: "purchase_orders",
                column: "VendorId",
                principalTable: "vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        
            // 1. Partial Unique Active Revision Index
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ix_po_unique_active_revision 
                ON purchase_orders (""TenantId"", ""PurchaseOrderNumber"") 
                WHERE ""IsCurrentRevision"" = TRUE AND ""IsDeleted"" = FALSE;
            ");

            // 2. Check Constraints
            migrationBuilder.Sql(@"
                DO $$ BEGIN 
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_po_exchange_rate_positive') THEN 
                        ALTER TABLE purchase_orders ADD CONSTRAINT chk_po_exchange_rate_positive CHECK (""ExchangeRate"" > 0); 
                    END IF; 
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$ BEGIN 
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_po_line_quantities_valid') THEN 
                        ALTER TABLE purchase_order_lines ADD CONSTRAINT chk_po_line_quantities_valid 
                        CHECK (""Quantity"" >= 0 AND ""ReceivedQuantity"" >= 0 AND ""RejectedQuantity"" >= 0 AND ""InvoicedQuantity"" >= 0 AND ""CancelledQuantity"" >= 0); 
                    END IF; 
                END $$;
            ");

            // 3. PostgreSQL Triggers for Line & Header Fulfillment Synchronization
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION fn_trg_set_po_line_status()
                RETURNS TRIGGER AS $$
                BEGIN
                    IF NEW.""CancelledQuantity"" >= NEW.""Quantity"" AND NEW.""Quantity"" > 0 THEN
                        NEW.""LineStatus"" := 'Cancelled';
                    ELSIF NEW.""ReceivedQuantity"" >= NEW.""Quantity"" AND NEW.""Quantity"" > 0 THEN
                        NEW.""LineStatus"" := 'FullyReceived';
                    ELSIF NEW.""ReceivedQuantity"" > 0 THEN
                        NEW.""LineStatus"" := 'PartiallyReceived';
                    ELSE
                        NEW.""LineStatus"" := 'Pending';
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                DROP TRIGGER IF EXISTS trg_po_line_status_calc ON purchase_order_lines;
                CREATE TRIGGER trg_po_line_status_calc
                BEFORE INSERT OR UPDATE OF ""Quantity"", ""ReceivedQuantity"", ""CancelledQuantity"" ON purchase_order_lines
                FOR EACH ROW EXECUTE FUNCTION fn_trg_set_po_line_status();
            ");

            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION fn_trg_sync_po_fulfillment_status()
                RETURNS TRIGGER AS $$
                DECLARE
                    v_po_id INT;
                    v_total_ordered NUMERIC(18,4);
                    v_total_received NUMERIC(18,4);
                    v_all_closed BOOLEAN;
                    v_has_partial BOOLEAN;
                    v_current_status VARCHAR(64);
                BEGIN
                    v_po_id := COALESCE(NEW.""PurchaseOrderId"", OLD.""PurchaseOrderId"");
                    IF v_po_id IS NULL THEN
                        RETURN NULL;
                    END IF;

                    SELECT ""Status"" INTO v_current_status FROM purchase_orders WHERE ""Id"" = v_po_id;
                    IF v_current_status IN ('Approved', 'SentToVendor', 'Acknowledged', 'PartiallyReceived', 'FullyReceived') THEN
                        SELECT 
                            COALESCE(SUM(""Quantity""), 0),
                            COALESCE(SUM(""ReceivedQuantity""), 0),
                            BOOL_AND(""ReceivedQuantity"" + ""CancelledQuantity"" >= ""Quantity""),
                            BOOL_OR(""ReceivedQuantity"" > 0)
                        INTO 
                            v_total_ordered,
                            v_total_received,
                            v_all_closed,
                            v_has_partial
                        FROM purchase_order_lines
                        WHERE ""PurchaseOrderId"" = v_po_id;

                        IF v_all_closed AND v_total_ordered > 0 THEN
                            UPDATE purchase_orders 
                            SET ""Status"" = 'FullyReceived', ""UpdatedAt"" = NOW() 
                            WHERE ""Id"" = v_po_id;
                        ELSIF v_has_partial THEN
                            UPDATE purchase_orders 
                            SET ""Status"" = 'PartiallyReceived', ""UpdatedAt"" = NOW() 
                            WHERE ""Id"" = v_po_id;
                        END IF;
                    END IF;

                    RETURN NULL;
                END;
                $$ LANGUAGE plpgsql;

                DROP TRIGGER IF EXISTS trg_po_fulfillment_sync ON purchase_order_lines;
                CREATE TRIGGER trg_po_fulfillment_sync
                AFTER INSERT OR UPDATE OF ""Quantity"", ""ReceivedQuantity"", ""CancelledQuantity"" OR DELETE ON purchase_order_lines
                FOR EACH ROW EXECUTE FUNCTION fn_trg_sync_po_fulfillment_status();
            ");
        
            // Recreate dependent view mv_vendor_procurement_summary
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
                name: "FK_purchase_order_lines_purchase_requisition_lines_PurchaseReq~",
                table: "purchase_order_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_purchase_order_lines_rfq_lines_RFQLineId",
                table: "purchase_order_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_purchase_order_lines_vendor_quotation_lines_VendorQuotation~",
                table: "purchase_order_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_purchase_orders_purchase_orders_ParentPOId",
                table: "purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_purchase_orders_purchase_requisitions_PurchaseRequisitionId",
                table: "purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_purchase_orders_request_for_quotations_RFQId",
                table: "purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_purchase_orders_vendor_comparisons_VendorComparisonId",
                table: "purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_purchase_orders_vendors_VendorId",
                table: "purchase_orders");

            migrationBuilder.DropTable(
                name: "purchase_order_amendment_histories");

            migrationBuilder.DropTable(
                name: "purchase_order_approvals");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_ParentPOId",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_PurchaseRequisitionId",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_RFQId",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_TenantId_Status_OrderDate",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_TenantId_Status_RequiredApprovalTier_Curren~",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_VendorComparisonId",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_order_lines_PurchaseOrderId_LineStatus",
                table: "purchase_order_lines");

            migrationBuilder.DropIndex(
                name: "IX_purchase_order_lines_PurchaseRequisitionLineId",
                table: "purchase_order_lines");

            migrationBuilder.DropIndex(
                name: "IX_purchase_order_lines_RFQLineId",
                table: "purchase_order_lines");

            migrationBuilder.DropIndex(
                name: "IX_purchase_order_lines_VendorQuotationLineId",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "AcknowledgedAt",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "AmendmentReason",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "AmendmentType",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "ApprovalToken",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "CurrentApprovalTier",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "ExceptionReasonsJson",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "FreightAmount",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "GrandTotalBaseCurrency",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "Incoterms",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "InsuranceAmount",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "IsCurrentRevision",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "IsExceptionApproval",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "ParentPOId",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "PaymentTermsDays",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "PurchaseRequisitionId",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "RFQId",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "RequiredApprovalTier",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "RevisionNumber",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "SupersededAt",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "TotalCgstAmount",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "TotalIgstAmount",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "TotalSgstAmount",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "VendorCode",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "VendorComparisonId",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "VendorGstin",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "VendorPan",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "VendorStateCode",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "BaseUom",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "CancelledQuantity",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "CgstAmount",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "DeliveryScheduleJson",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "ExpectedDeliveryDate",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "GstRatePercentage",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "HsnCode",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "IgstAmount",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "InvoicedQuantity",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "IsItcEligible",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "ItemCode",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "LineNumber",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "LineStatus",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "LineTotal",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "OrderedQuantity",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "OverDeliveryTolerancePercent",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "PurchaseRequisitionLineId",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "RFQLineId",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "RejectedQuantity",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "SgstAmount",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "TargetWarehouseId",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "TaxableAmount",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "UnitRate",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "VendorQuotationLineId",
                table: "purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "purchase_order_lines");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "purchase_orders",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "purchase_orders",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxTotal",
                table: "purchase_orders",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "Subtotal",
                table: "purchase_orders",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "ShippingAddress",
                table: "purchase_orders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "SalesOrderNumber",
                table: "purchase_orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Remarks",
                table: "purchase_orders",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "purchase_orders",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "GstNumber",
                table: "purchase_orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountTotal",
                table: "purchase_orders",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "purchase_orders",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "BuyerName",
                table: "purchase_orders",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "BillingAddress",
                table: "purchase_orders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "purchase_order_lines",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Nos",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldDefaultValue: "Nos");

            migrationBuilder.AlterColumn<decimal>(
                name: "Tax",
                table: "purchase_order_lines",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "purchase_order_lines",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<decimal>(
                name: "BilledQuantity",
                table: "purchase_order_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "purchase_order_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_PurchaseOrderNumber",
                table: "purchase_orders",
                column: "PurchaseOrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_Status_IsDeleted_OrderDate",
                table: "purchase_orders",
                columns: new[] { "Status", "IsDeleted", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_VendorId_Status_OrderDate",
                table: "purchase_orders",
                columns: new[] { "VendorId", "Status", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_UomId",
                table: "purchase_order_lines",
                column: "UomId");

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_orders_vendors_VendorId",
                table: "purchase_orders",
                column: "VendorId",
                principalTable: "vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
