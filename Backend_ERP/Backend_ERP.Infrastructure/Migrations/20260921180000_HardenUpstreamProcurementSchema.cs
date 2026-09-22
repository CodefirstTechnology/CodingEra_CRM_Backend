using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    [DbContext(typeof(ERPDbContext))]
    [Migration("20260921180000_HardenUpstreamProcurementSchema")]
    public partial class HardenUpstreamProcurementSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    -- 1. Add ItemId and UomId to purchase_requisition_lines
                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables WHERE table_name = 'purchase_requisition_lines'
                    ) THEN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns
                            WHERE table_name = 'purchase_requisition_lines' AND (column_name = 'ItemId' OR column_name = 'item_id')
                        ) THEN
                            ALTER TABLE purchase_requisition_lines ADD COLUMN "ItemId" integer NULL;
                        END IF;

                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns
                            WHERE table_name = 'purchase_requisition_lines' AND (column_name = 'UomId' OR column_name = 'uom_id')
                        ) THEN
                            ALTER TABLE purchase_requisition_lines ADD COLUMN "UomId" integer NULL;
                        END IF;
                    END IF;

                    -- 2. Add ItemId, UomId, PurchaseRequisitionLineId to rfq_lines
                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables WHERE table_name = 'rfq_lines'
                    ) THEN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns
                            WHERE table_name = 'rfq_lines' AND (column_name = 'ItemId' OR column_name = 'item_id')
                        ) THEN
                            ALTER TABLE rfq_lines ADD COLUMN "ItemId" integer NULL;
                        END IF;

                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns
                            WHERE table_name = 'rfq_lines' AND (column_name = 'UomId' OR column_name = 'uom_id')
                        ) THEN
                            ALTER TABLE rfq_lines ADD COLUMN "UomId" integer NULL;
                        END IF;

                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns
                            WHERE table_name = 'rfq_lines' AND (column_name = 'PurchaseRequisitionLineId' OR column_name = 'purchase_requisition_line_id')
                        ) THEN
                            ALTER TABLE rfq_lines ADD COLUMN "PurchaseRequisitionLineId" integer NULL;
                        END IF;
                    END IF;

                    -- 3. Add ItemId, UomId, RequestForQuotationLineId to vendor_quotation_lines
                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables WHERE table_name = 'vendor_quotation_lines'
                    ) THEN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns
                            WHERE table_name = 'vendor_quotation_lines' AND (column_name = 'ItemId' OR column_name = 'item_id')
                        ) THEN
                            ALTER TABLE vendor_quotation_lines ADD COLUMN "ItemId" integer NULL;
                        END IF;

                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns
                            WHERE table_name = 'vendor_quotation_lines' AND (column_name = 'UomId' OR column_name = 'uom_id')
                        ) THEN
                            ALTER TABLE vendor_quotation_lines ADD COLUMN "UomId" integer NULL;
                        END IF;

                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.columns
                            WHERE table_name = 'vendor_quotation_lines' AND (column_name = 'RequestForQuotationLineId' OR column_name = 'request_for_quotation_line_id')
                        ) THEN
                            ALTER TABLE vendor_quotation_lines ADD COLUMN "RequestForQuotationLineId" integer NULL;
                        END IF;
                    END IF;

                    -- 4. Create vendor_comparison_lines table for split-award matrix
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'public' AND table_name = 'vendor_comparison_lines'
                    ) THEN
                        CREATE TABLE vendor_comparison_lines (
                            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                            "VendorComparisonId" integer NOT NULL REFERENCES vendor_comparisons ("Id") ON DELETE CASCADE,
                            "ItemId" integer NULL,
                            "ItemName" character varying(256) NOT NULL,
                            "RequestForQuotationLineId" integer NULL REFERENCES rfq_lines ("Id") ON DELETE SET NULL,
                            "Quantity" numeric(18,4) NOT NULL DEFAULT 0.0,
                            "Uom" character varying(64) NOT NULL DEFAULT 'PCS',
                            "LowestUnitPrice" numeric(18,4) NOT NULL DEFAULT 0.0,
                            "LowestVendorId" integer NULL REFERENCES vendors ("Id") ON DELETE SET NULL,
                            "LowestVendorName" character varying(256) NOT NULL DEFAULT '',
                            "IsSplitAwarded" boolean NOT NULL DEFAULT false,
                            "Remarks" character varying(1000) NOT NULL DEFAULT ''
                        );

                        CREATE INDEX "IX_vendor_comparison_lines_VendorComparisonId" ON vendor_comparison_lines ("VendorComparisonId");
                    END IF;

                    -- 5. Foreign Key Constraints (Idempotent)
                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'rfq_lines') THEN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.table_constraints
                            WHERE constraint_name = 'FK_rfq_lines_pr_lines'
                        ) THEN
                            ALTER TABLE rfq_lines
                            ADD CONSTRAINT "FK_rfq_lines_pr_lines"
                            FOREIGN KEY ("PurchaseRequisitionLineId") REFERENCES purchase_requisition_lines ("Id") ON DELETE SET NULL;
                        END IF;
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'vendor_quotation_lines') THEN
                        IF NOT EXISTS (
                            SELECT 1 FROM information_schema.table_constraints
                            WHERE constraint_name = 'FK_vendor_quote_lines_rfq_lines'
                        ) THEN
                            ALTER TABLE vendor_quotation_lines
                            ADD CONSTRAINT "FK_vendor_quote_lines_rfq_lines"
                            FOREIGN KEY ("RequestForQuotationLineId") REFERENCES rfq_lines ("Id") ON DELETE SET NULL;
                        END IF;
                    END IF;

                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TABLE IF EXISTS vendor_comparison_lines;
                """);
        }
    }
}
