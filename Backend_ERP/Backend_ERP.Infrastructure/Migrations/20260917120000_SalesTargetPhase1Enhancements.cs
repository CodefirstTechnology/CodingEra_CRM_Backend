using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    [DbContext(typeof(ERPDbContext))]
    [Migration("20260917120000_SalesTargetPhase1Enhancements")]
    public partial class SalesTargetPhase1Enhancements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- 1. Extend sales_targets table
ALTER TABLE public.sales_targets 
ADD COLUMN IF NOT EXISTS ""OverAchievementValue"" numeric(18,2) NOT NULL DEFAULT 0.00,
ADD COLUMN IF NOT EXISTS ""IsLocked"" boolean NOT NULL DEFAULT false,
ADD COLUMN IF NOT EXISTS ""RevisionNumber"" integer NOT NULL DEFAULT 1;

-- 2. Create Realization Tracking Table (Prevents double counting & tracks reversals)
CREATE TABLE IF NOT EXISTS public.sales_target_realizations (
    ""Id"" serial PRIMARY KEY,
    ""SalesTargetId"" integer NOT NULL REFERENCES public.sales_targets(""Id"") ON DELETE CASCADE,
    ""SalesOrderId"" integer NULL,
    ""InvoiceId"" integer NULL,
    ""TransactionType"" varchar(32) NOT NULL DEFAULT 'SalesOrder',
    ""RealizedAmount"" numeric(18,2) NOT NULL,
    ""IsReversal"" boolean NOT NULL DEFAULT false,
    ""AppliedOn"" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ""AppliedBy"" varchar(100) NOT NULL DEFAULT 'SYSTEM_EVENT'
);

-- 3. Defensive Composite Indexes
CREATE INDEX IF NOT EXISTS ""IX_sales_target_realizations_lookup"" 
ON public.sales_target_realizations (""SalesTargetId"", ""SalesOrderId"", ""IsReversal"");

CREATE INDEX IF NOT EXISTS ""IX_sales_targets_active_window"" 
ON public.sales_targets (""Status"", ""SalesPersonUserId"", ""TargetCategory"", ""StartDate"", ""EndDate"", ""IsDeleted"");
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS public.sales_target_realizations CASCADE;

ALTER TABLE public.sales_targets 
DROP COLUMN IF EXISTS ""OverAchievementValue"",
DROP COLUMN IF EXISTS ""IsLocked"",
DROP COLUMN IF EXISTS ""RevisionNumber"";
");
        }
    }
}
