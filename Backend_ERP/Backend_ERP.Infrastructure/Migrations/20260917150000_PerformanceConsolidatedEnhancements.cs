using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Infrastructure.Migrations
{
    public partial class PerformanceConsolidatedEnhancements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- 0. Ensure base table salesperson_performances exists
CREATE TABLE IF NOT EXISTS public.salesperson_performances (
    ""Id"" serial PRIMARY KEY,
    ""SalesPersonUserId"" integer NOT NULL,
    ""SalesPersonName"" varchar(150) NOT NULL DEFAULT '',
    ""SalesTeam"" varchar(100) NULL,
    ""Branch"" varchar(100) NULL,
    ""RegionalManager"" varchar(100) NULL,
    ""FinancialYear"" integer NOT NULL DEFAULT 2026,
    ""TotalTargetValue"" numeric(18,2) NOT NULL DEFAULT 0.00,
    ""TotalAchievedValue"" numeric(18,2) NOT NULL DEFAULT 0.00,
    ""AttainmentPercentage"" numeric(5,2) NOT NULL DEFAULT 0.00,
    ""ConfirmedOrderCount"" integer NOT NULL DEFAULT 0,
    ""TotalQuotationCount"" integer NOT NULL DEFAULT 0,
    ""ConversionRate"" numeric(5,2) NOT NULL DEFAULT 0.00,
    ""CalculatedCommission"" numeric(18,2) NOT NULL DEFAULT 0.00,
    ""Status"" varchar(50) NOT NULL DEFAULT 'Active',
    ""CreatedDate"" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ""UpdatedDate"" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 1. Extend salesperson_performances with balanced scoring & gamification fields
ALTER TABLE public.salesperson_performances 
ADD COLUMN IF NOT EXISTS ""WeightedScore"" numeric(5,2) NOT NULL DEFAULT 0.00,
ADD COLUMN IF NOT EXISTS ""GrossMarginPercentage"" numeric(5,2) NOT NULL DEFAULT 0.00,
ADD COLUMN IF NOT EXISTS ""AvgDealVelocityDays"" numeric(5,1) NOT NULL DEFAULT 0.0,
ADD COLUMN IF NOT EXISTS ""ActiveBadgesJson"" text NOT NULL DEFAULT '[]',
ADD COLUMN IF NOT EXISTS ""PeriodType"" varchar(20) NOT NULL DEFAULT 'FiscalYear';

-- 2. Create historical snapshots table for auditing and MoM/YoY trend tracking
CREATE TABLE IF NOT EXISTS public.salesperson_performance_snapshots (
    ""Id"" serial PRIMARY KEY,
    ""SalesPersonUserId"" integer NOT NULL,
    ""SalesPersonName"" varchar(150) NOT NULL,
    ""Branch"" varchar(100) NULL,
    ""SalesTeam"" varchar(100) NULL,
    ""FinancialYear"" integer NOT NULL,
    ""PeriodKey"" varchar(20) NOT NULL,
    ""Rank"" integer NOT NULL,
    ""WeightedScore"" numeric(5,2) NOT NULL DEFAULT 0.00,
    ""AttainmentPercentage"" numeric(5,2) NOT NULL DEFAULT 0.00,
    ""TotalAchievedValue"" numeric(18,2) NOT NULL DEFAULT 0.00,
    ""TotalTargetValue"" numeric(18,2) NOT NULL DEFAULT 0.00,
    ""CalculatedCommission"" numeric(18,2) NOT NULL DEFAULT 0.00,
    ""ActiveBadgesJson"" text NOT NULL DEFAULT '[]',
    ""SnapshotDate"" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 3. High-performance composite indexes
CREATE INDEX IF NOT EXISTS ""IX_salesperson_performances_leaderboard"" 
ON public.salesperson_performances (""FinancialYear"", ""WeightedScore"" DESC, ""TotalAchievedValue"" DESC);

CREATE INDEX IF NOT EXISTS ""IX_salesperson_snapshots_user_period"" 
ON public.salesperson_performance_snapshots (""SalesPersonUserId"", ""PeriodKey"");
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS public.""IX_salesperson_snapshots_user_period"";
DROP INDEX IF EXISTS public.""IX_salesperson_performances_leaderboard"";
DROP TABLE IF EXISTS public.salesperson_performance_snapshots;
ALTER TABLE public.salesperson_performances 
DROP COLUMN IF EXISTS ""PeriodType"",
DROP COLUMN IF EXISTS ""ActiveBadgesJson"",
DROP COLUMN IF EXISTS ""AvgDealVelocityDays"",
DROP COLUMN IF EXISTS ""GrossMarginPercentage"",
DROP COLUMN IF EXISTS ""WeightedScore"";
");
        }
    }
}
