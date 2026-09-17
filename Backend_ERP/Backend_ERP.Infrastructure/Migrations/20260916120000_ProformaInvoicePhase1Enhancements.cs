using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    [DbContext(typeof(ERPDbContext))]
    [Migration("20260916120000_ProformaInvoicePhase1Enhancements")]
    public partial class ProformaInvoicePhase1Enhancements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- 1. Sales Order Tracking: Prevent Over-Invoicing
ALTER TABLE public.sales_orders 
ADD COLUMN IF NOT EXISTS ""ProformaInvoicedAmount"" numeric(18,2) NOT NULL DEFAULT 0.00;

ALTER TABLE public.sales_order_items 
ADD COLUMN IF NOT EXISTS ""ProformaInvoicedQuantity"" numeric(18,4) NOT NULL DEFAULT 0.0000;

-- 2. Proforma Invoice Header: Milestone & Commercial Risk Fields
ALTER TABLE public.proforma_invoices 
ADD COLUMN IF NOT EXISTS ""BillingType"" varchar(32) NOT NULL DEFAULT 'Full',
ADD COLUMN IF NOT EXISTS ""MilestonePercentage"" numeric(5,2) NULL,
ADD COLUMN IF NOT EXISTS ""RequiresFinanceCreditReview"" boolean NOT NULL DEFAULT false,
ADD COLUMN IF NOT EXISTS ""CreditReviewReason"" text NULL;

-- 3. Composite Indexes for Background Worker & Performance
CREATE INDEX IF NOT EXISTS ""IX_proforma_invoices_expiry_scan"" 
ON public.proforma_invoices (""Status"", ""ValidUntil"", ""AdvanceReceivedAmount"", ""IsDeleted"");

CREATE INDEX IF NOT EXISTS ""IX_proforma_invoices_so_lookup"" 
ON public.proforma_invoices (""SalesOrderId"", ""Status"", ""IsDeleted"");
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS public.""IX_proforma_invoices_so_lookup"";
DROP INDEX IF EXISTS public.""IX_proforma_invoices_expiry_scan"";

ALTER TABLE public.proforma_invoices 
DROP COLUMN IF EXISTS ""CreditReviewReason"",
DROP COLUMN IF EXISTS ""RequiresFinanceCreditReview"",
DROP COLUMN IF EXISTS ""MilestonePercentage"",
DROP COLUMN IF EXISTS ""BillingType"";

ALTER TABLE public.sales_order_items 
DROP COLUMN IF EXISTS ""ProformaInvoicedQuantity"";

ALTER TABLE public.sales_orders 
DROP COLUMN IF EXISTS ""ProformaInvoicedAmount"";
");
        }
    }
}
