using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    [DbContext(typeof(ERPDbContext))]
    [Migration("20260916140000_ProformaInvoicePhase2StaggeredConversions")]
    public partial class ProformaInvoicePhase2StaggeredConversions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- 1. Line Item Conversion Tracking
ALTER TABLE public.proforma_invoice_items 
ADD COLUMN IF NOT EXISTS ""ConvertedQuantity"" numeric(18,4) NOT NULL DEFAULT 0.0000;

-- 2. Header State & Conversion Metadata
ALTER TABLE public.proforma_invoices 
ADD COLUMN IF NOT EXISTS ""IsProductionReleased"" boolean NOT NULL DEFAULT false,
ADD COLUMN IF NOT EXISTS ""ProductionReleasedOn"" timestamptz NULL;

-- 3. Composite Index for Conversion and Settlement Lookups
CREATE INDEX IF NOT EXISTS ""IX_pi_items_conversion_tracking"" 
ON public.proforma_invoice_items (""ProformaInvoiceId"", ""Quantity"", ""ConvertedQuantity"");

CREATE INDEX IF NOT EXISTS ""IX_pi_settlement_production_release"" 
ON public.proforma_invoices (""PaymentStatus"", ""IsProductionReleased"", ""IsDeleted"");
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS public.""IX_pi_settlement_production_release"";
DROP INDEX IF EXISTS public.""IX_pi_items_conversion_tracking"";

ALTER TABLE public.proforma_invoices 
DROP COLUMN IF EXISTS ""ProductionReleasedOn"",
DROP COLUMN IF EXISTS ""IsProductionReleased"";

ALTER TABLE public.proforma_invoice_items 
DROP COLUMN IF EXISTS ""ConvertedQuantity"";
");
        }
    }
}
