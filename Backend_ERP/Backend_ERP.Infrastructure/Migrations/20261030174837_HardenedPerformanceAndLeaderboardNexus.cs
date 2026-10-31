using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardenedPerformanceAndLeaderboardNexus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'public' AND (tablename = 'salesperson_performances' OR tablename = 'SalespersonPerformances')) THEN
                        CREATE TABLE public.salesperson_performances (
                            ""Id"" serial PRIMARY KEY,
                            ""SalesPersonUserId"" integer NOT NULL,
                            ""SalesPersonName"" varchar(256) NOT NULL DEFAULT '',
                            ""SalesTeam"" varchar(128) NULL,
                            ""Branch"" varchar(128) NULL,
                            ""RegionalManager"" varchar(128) NULL,
                            ""FinancialYear"" integer NOT NULL DEFAULT 2026,
                            ""TotalTargetValue"" numeric(18,2) NOT NULL DEFAULT 0.00,
                            ""TotalAchievedValue"" numeric(18,2) NOT NULL DEFAULT 0.00,
                            ""AttainmentPercentage"" numeric(7,2) NOT NULL DEFAULT 0.00,
                            ""ConfirmedOrderCount"" integer NOT NULL DEFAULT 0,
                            ""TotalQuotationCount"" integer NOT NULL DEFAULT 0,
                            ""ConversionRate"" numeric(7,2) NOT NULL DEFAULT 0.00,
                            ""CalculatedCommission"" numeric(18,2) NOT NULL DEFAULT 0.00,
                            ""Status"" varchar(32) NOT NULL DEFAULT 'Active',
                            ""CreatedDate"" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            ""UpdatedDate"" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            ""WeightedScore"" numeric(7,2) NOT NULL DEFAULT 0.00,
                            ""GrossMarginPercentage"" numeric(7,2) NOT NULL DEFAULT 0.00,
                            ""AvgDealVelocityDays"" numeric(7,2) NOT NULL DEFAULT 0.0,
                            ""ActiveBadgesJson"" text NOT NULL DEFAULT '[]',
                            ""PeriodType"" varchar(32) NOT NULL DEFAULT 'FiscalYear'
                        );
                    ELSIF EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'public' AND tablename = 'SalespersonPerformances') THEN
                        ALTER TABLE public.""SalespersonPerformances"" RENAME TO salesperson_performances;
                    END IF;
                END $$;
            ");

            migrationBuilder.AlterColumn<decimal>(
                name: "WeightedScore",
                table: "salesperson_performances",
                type: "numeric(7,2)",
                precision: 7,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalTargetValue",
                table: "salesperson_performances",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "salesperson_performances",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Active",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "SalesTeam",
                table: "salesperson_performances",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SalesPersonName",
                table: "salesperson_performances",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "RegionalManager",
                table: "salesperson_performances",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PeriodType",
                table: "salesperson_performances",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "FiscalYear",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "GrossMarginPercentage",
                table: "salesperson_performances",
                type: "numeric(7,2)",
                precision: 7,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "ConversionRate",
                table: "salesperson_performances",
                type: "numeric(7,2)",
                precision: 7,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "Branch",
                table: "salesperson_performances",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "AvgDealVelocityDays",
                table: "salesperson_performances",
                type: "numeric(7,2)",
                precision: 7,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "AttainmentPercentage",
                table: "salesperson_performances",
                type: "numeric(7,2)",
                precision: 7,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<int>(
                name: "CurrentRank",
                table: "salesperson_performances",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountDisciplineScore",
                table: "salesperson_performances",
                type: "numeric(7,2)",
                precision: 7,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PacingIndex",
                table: "salesperson_performances",
                type: "numeric(7,2)",
                precision: 7,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PreviousRank",
                table: "salesperson_performances",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "salesperson_performances",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "default");

            migrationBuilder.AddColumn<int>(
                name: "TerritoryId",
                table: "salesperson_performances",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TierBadge",
                table: "salesperson_performances",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Silver");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAccruedCommission",
                table: "salesperson_performances",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalBookedRevenue",
                table: "salesperson_performances",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCashCollected",
                table: "salesperson_performances",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalClawbackCommission",
                table: "salesperson_performances",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalDispatchedRevenue",
                table: "salesperson_performances",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalGrossMarginAmount",
                table: "salesperson_performances",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPaidCommission",
                table: "salesperson_performances",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "WinningStreakMonths",
                table: "salesperson_performances",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "salesperson_performances",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'PK_salesperson_performances') 
                       AND NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'salesperson_performances_pkey') THEN
                        ALTER TABLE public.salesperson_performances ADD CONSTRAINT ""PK_salesperson_performances"" PRIMARY KEY (""Id"");
                    END IF;
                END $$;
            ");
            migrationBuilder.CreateTable(
                name: "sales_incentive_schemes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "default"),
                    SchemeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SchemeName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FiscalYear = table.Column<int>(type: "integer", nullable: false),
                    PeriodType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Monthly"),
                    BaseCommissionRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    MarginHurdlePercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    MinimumAttainmentThreshold = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    AcceleratorTiersJson = table.Column<string>(type: "jsonb", nullable: false),
                    RequiresCashClearance = table.Column<bool>(type: "boolean", nullable: false),
                    MinCashClearancePercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Active"),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_incentive_schemes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sales_leaderboard_snapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "default"),
                    FinancialYear = table.Column<int>(type: "integer", nullable: false),
                    PeriodKey = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PartitionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Global"),
                    PartitionValue = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, defaultValue: "ALL"),
                    SalesPersonUserId = table.Column<int>(type: "integer", nullable: false),
                    SalesPersonName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    WeightedScore = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    AttainmentPercentage = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    TotalBookedRevenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCashCollected = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossMarginPercentage = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    TierBadge = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Silver"),
                    SnapshotDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_leaderboard_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sales_rep_badges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "default"),
                    ProfileId = table.Column<int>(type: "integer", nullable: false),
                    SalesPersonUserId = table.Column<int>(type: "integer", nullable: false),
                    BadgeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BadgeTitle = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    BadgeCategory = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "Quota"),
                    Icon = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "trophy"),
                    Color = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "emerald"),
                    EvaluationCriteriaSnapshot = table.Column<string>(type: "jsonb", nullable: false),
                    AwardedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_rep_badges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sales_rep_badges_salesperson_performances_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "salesperson_performances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sales_commission_ledgers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "default"),
                    ProfileId = table.Column<int>(type: "integer", nullable: false),
                    SalesPersonUserId = table.Column<int>(type: "integer", nullable: false),
                    IncentiveSchemeId = table.Column<int>(type: "integer", nullable: true),
                    SalesOrderId = table.Column<int>(type: "integer", nullable: true),
                    AdvancePaymentId = table.Column<int>(type: "integer", nullable: true),
                    SalesInvoiceId = table.Column<int>(type: "integer", nullable: true),
                    TransactionType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TransactionAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    AcceleratorMultiplier = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    AccruedCommission = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsClawback = table.Column<bool>(type: "boolean", nullable: false),
                    ReversalOfLedgerId = table.Column<int>(type: "integer", nullable: true),
                    ClawbackReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    PayoutStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Accrued"),
                    ApprovalToken = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PaymentVoucherReference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    AccruedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PayoutDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_commission_ledgers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sales_commission_ledgers_sales_incentive_schemes_IncentiveS~",
                        column: x => x.IncentiveSchemeId,
                        principalTable: "sales_incentive_schemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_sales_commission_ledgers_salesperson_performances_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "salesperson_performances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_perf_profiles_branch_fy",
                table: "salesperson_performances",
                columns: new[] { "TenantId", "Branch", "FinancialYear" });

            migrationBuilder.CreateIndex(
                name: "ix_perf_profiles_leaderboard_rank",
                table: "salesperson_performances",
                columns: new[] { "TenantId", "FinancialYear", "WeightedScore" });

            migrationBuilder.CreateIndex(
                name: "ix_perf_profiles_team_fy",
                table: "salesperson_performances",
                columns: new[] { "TenantId", "SalesTeam", "FinancialYear" });

            migrationBuilder.CreateIndex(
                name: "ix_perf_profiles_tenant_fy_user",
                table: "salesperson_performances",
                columns: new[] { "TenantId", "FinancialYear", "SalesPersonUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_commission_ledger_order",
                table: "sales_commission_ledgers",
                columns: new[] { "TenantId", "SalesOrderId" });

            migrationBuilder.CreateIndex(
                name: "ix_commission_ledger_rep_status",
                table: "sales_commission_ledgers",
                columns: new[] { "TenantId", "SalesPersonUserId", "PayoutStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_commission_ledgers_IncentiveSchemeId",
                table: "sales_commission_ledgers",
                column: "IncentiveSchemeId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_commission_ledgers_ProfileId",
                table: "sales_commission_ledgers",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "ix_incentive_schemes_tenant_code_fy",
                table: "sales_incentive_schemes",
                columns: new[] { "TenantId", "SchemeCode", "FiscalYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_leaderboard_snapshots_period_rank",
                table: "sales_leaderboard_snapshots",
                columns: new[] { "TenantId", "FinancialYear", "PeriodKey", "PartitionType", "PartitionValue", "Rank" });

            migrationBuilder.CreateIndex(
                name: "ix_rep_badges_user_code",
                table: "sales_rep_badges",
                columns: new[] { "TenantId", "SalesPersonUserId", "BadgeCode" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_rep_badges_ProfileId",
                table: "sales_rep_badges",
                column: "ProfileId");
            migrationBuilder.Sql(@"
                DO $block$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_perf_margin_percentage_bounds') THEN
                        ALTER TABLE public.salesperson_performances 
                        ADD CONSTRAINT chk_perf_margin_percentage_bounds CHECK (""GrossMarginPercentage"" BETWEEN -100.00 AND 100.00);
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_incentive_base_rate_positive') THEN
                        ALTER TABLE public.sales_incentive_schemes 
                        ADD CONSTRAINT chk_incentive_base_rate_positive CHECK (""BaseCommissionRate"" >= 0);
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_commission_accrual_nonnegative') THEN
                        ALTER TABLE public.sales_commission_ledgers 
                        ADD CONSTRAINT chk_commission_accrual_nonnegative CHECK (""AccruedCommission"" >= 0);
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_proc WHERE proname = 'fn_trg_sync_rep_commission_totals') THEN
                        CREATE OR REPLACE FUNCTION fn_trg_sync_rep_commission_totals()
                        RETURNS TRIGGER AS $func$
                        DECLARE
                            v_profile_id INT;
                            v_total_accrued NUMERIC(18,2);
                            v_total_clawback NUMERIC(18,2);
                            v_total_paid NUMERIC(18,2);
                        BEGIN
                            v_profile_id := COALESCE(NEW.""ProfileId"", OLD.""ProfileId"");

                            PERFORM 1 FROM public.salesperson_performances WHERE ""Id"" = v_profile_id FOR UPDATE;

                            SELECT 
                                COALESCE(SUM(CASE WHEN NOT ""IsClawback"" AND ""TransactionType"" != 'PayoutDisbursement' THEN ""AccruedCommission"" ELSE 0 END), 0),
                                COALESCE(SUM(CASE WHEN ""IsClawback"" THEN ABS(""AccruedCommission"") ELSE 0 END), 0),
                                COALESCE(SUM(CASE WHEN ""TransactionType"" = 'PayoutDisbursement' THEN ""AccruedCommission"" ELSE 0 END), 0)
                            INTO 
                                v_total_accrued,
                                v_total_clawback,
                                v_total_paid
                            FROM public.sales_commission_ledgers
                            WHERE ""ProfileId"" = v_profile_id;

                            UPDATE public.salesperson_performances
                            SET 
                                ""TotalAccruedCommission"" = v_total_accrued,
                                ""TotalClawbackCommission"" = v_total_clawback,
                                ""TotalPaidCommission"" = v_total_paid,
                                ""UpdatedDate"" = CURRENT_TIMESTAMP
                            WHERE ""Id"" = v_profile_id;

                            RETURN NEW;
                        END;
                        $func$ LANGUAGE plpgsql;

                        DROP TRIGGER IF EXISTS trg_sync_rep_commission_totals ON public.sales_commission_ledgers;
                        CREATE TRIGGER trg_sync_rep_commission_totals
                        AFTER INSERT OR UPDATE OR DELETE ON public.sales_commission_ledgers
                        FOR EACH ROW
                        EXECUTE FUNCTION fn_trg_sync_rep_commission_totals();
                    END IF;
                END $block$;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sales_commission_ledgers");

            migrationBuilder.DropTable(
                name: "sales_leaderboard_snapshots");

            migrationBuilder.DropTable(
                name: "sales_rep_badges");

            migrationBuilder.DropTable(
                name: "sales_incentive_schemes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_salesperson_performances",
                table: "salesperson_performances");

            migrationBuilder.DropIndex(
                name: "ix_perf_profiles_branch_fy",
                table: "salesperson_performances");

            migrationBuilder.DropIndex(
                name: "ix_perf_profiles_leaderboard_rank",
                table: "salesperson_performances");

            migrationBuilder.DropIndex(
                name: "ix_perf_profiles_team_fy",
                table: "salesperson_performances");

            migrationBuilder.DropIndex(
                name: "ix_perf_profiles_tenant_fy_user",
                table: "salesperson_performances");

            migrationBuilder.DropColumn(
                name: "CurrentRank",
                table: "salesperson_performances");

            migrationBuilder.DropColumn(
                name: "DiscountDisciplineScore",
                table: "salesperson_performances");

            migrationBuilder.DropColumn(
                name: "PacingIndex",
                table: "salesperson_performances");

            migrationBuilder.DropColumn(
                name: "PreviousRank",
                table: "salesperson_performances");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "salesperson_performances");

            migrationBuilder.DropColumn(
                name: "TerritoryId",
                table: "salesperson_performances");

            migrationBuilder.DropColumn(
                name: "TierBadge",
                table: "salesperson_performances");

            migrationBuilder.DropColumn(
                name: "TotalAccruedCommission",
                table: "salesperson_performances");

            migrationBuilder.DropColumn(
                name: "TotalBookedRevenue",
                table: "salesperson_performances");

            migrationBuilder.DropColumn(
                name: "TotalCashCollected",
                table: "salesperson_performances");

            migrationBuilder.DropColumn(
                name: "TotalClawbackCommission",
                table: "salesperson_performances");

            migrationBuilder.DropColumn(
                name: "TotalDispatchedRevenue",
                table: "salesperson_performances");

            migrationBuilder.DropColumn(
                name: "TotalGrossMarginAmount",
                table: "salesperson_performances");

            migrationBuilder.DropColumn(
                name: "TotalPaidCommission",
                table: "salesperson_performances");

            migrationBuilder.DropColumn(
                name: "WinningStreakMonths",
                table: "salesperson_performances");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "salesperson_performances");

            migrationBuilder.RenameTable(
                name: "salesperson_performances",
                newName: "SalespersonPerformances");

            migrationBuilder.AlterColumn<decimal>(
                name: "WeightedScore",
                table: "SalespersonPerformances",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(7,2)",
                oldPrecision: 7,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalTargetValue",
                table: "SalespersonPerformances",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "SalespersonPerformances",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldDefaultValue: "Active");

            migrationBuilder.AlterColumn<string>(
                name: "SalesTeam",
                table: "SalespersonPerformances",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SalesPersonName",
                table: "SalespersonPerformances",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "RegionalManager",
                table: "SalespersonPerformances",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PeriodType",
                table: "SalespersonPerformances",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldDefaultValue: "FiscalYear");

            migrationBuilder.AlterColumn<decimal>(
                name: "GrossMarginPercentage",
                table: "SalespersonPerformances",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(7,2)",
                oldPrecision: 7,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "ConversionRate",
                table: "SalespersonPerformances",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(7,2)",
                oldPrecision: 7,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Branch",
                table: "SalespersonPerformances",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "AvgDealVelocityDays",
                table: "SalespersonPerformances",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(7,2)",
                oldPrecision: 7,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "AttainmentPercentage",
                table: "SalespersonPerformances",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(7,2)",
                oldPrecision: 7,
                oldScale: 2);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SalespersonPerformances",
                table: "SalespersonPerformances",
                column: "Id");
        }
    }
}
