using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    [DbContext(typeof(ERPDbContext))]
    [Migration("20260917140000_SalesTargetPhase2HierarchyAndCommissions")]
    public partial class SalesTargetPhase2HierarchyAndCommissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- 1. Self-referencing Parent Target for Hierarchy & Proration tracking
ALTER TABLE public.sales_targets 
ADD COLUMN IF NOT EXISTS ""ParentTargetId"" integer NULL REFERENCES public.sales_targets(""Id"") ON DELETE SET NULL,
ADD COLUMN IF NOT EXISTS ""IsAutoAggregated"" boolean NOT NULL DEFAULT false,
ADD COLUMN IF NOT EXISTS ""OriginalTargetValue"" numeric(18,2) NULL,
ADD COLUMN IF NOT EXISTS ""ProrationFactor"" numeric(5,4) NOT NULL DEFAULT 1.0000,
ADD COLUMN IF NOT EXISTS ""CalculatedCommissionAmount"" numeric(18,2) NOT NULL DEFAULT 0.00;

-- 2. Performance index for hierarchical roll-up queries
CREATE INDEX IF NOT EXISTS ""IX_sales_targets_parent_hierarchy"" 
ON public.sales_targets (""ParentTargetId"", ""Status"", ""IsDeleted"");

-- 3. Ensure sales_orders has SalesPersonUserId for event-driven targets
ALTER TABLE public.sales_orders 
ADD COLUMN IF NOT EXISTS ""SalesPersonUserId"" integer NULL;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS public.""IX_sales_targets_parent_hierarchy"";

ALTER TABLE public.sales_targets 
DROP COLUMN IF EXISTS ""ParentTargetId"",
DROP COLUMN IF EXISTS ""IsAutoAggregated"",
DROP COLUMN IF EXISTS ""OriginalTargetValue"",
DROP COLUMN IF EXISTS ""ProrationFactor"",
DROP COLUMN IF EXISTS ""CalculatedCommissionAmount"";
");
        }
    }
}
