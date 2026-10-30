using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardenedSalesTargetsNexus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE public.sales_targets DROP CONSTRAINT IF EXISTS ""FK_sales_targets_sales_targets_ParentTargetId"";
                ALTER TABLE public.sales_targets DROP CONSTRAINT IF EXISTS ""sales_targets_ParentTargetId_fkey"";
                DROP INDEX IF EXISTS public.""IX_sales_targets_active_window"";
                DROP INDEX IF EXISTS public.""IX_sales_targets_FinancialYear"";
                DROP INDEX IF EXISTS public.""IX_sales_targets_parent_hierarchy"";
                DROP INDEX IF EXISTS public.""IX_sales_targets_TargetNumber"";
                DROP INDEX IF EXISTS public.""IX_sales_target_realizations_lookup"";
            ");
            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "sales_targets",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "TargetValue",
                table: "sales_targets",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "TargetType",
                table: "sales_targets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<decimal>(
                name: "RemainingValue",
                table: "sales_targets",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "OverAchievementValue",
                table: "sales_targets",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "sales_targets",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "INR",
                oldClrType: typeof(string),
                oldType: "character varying(8)",
                oldMaxLength: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "AchievementPercentage",
                table: "sales_targets",
                type: "numeric(9,2)",
                precision: 9,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(9,2)",
                oldPrecision: 9,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "AchievedValue",
                table: "sales_targets",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<decimal>(
                name: "AchievedVolume",
                table: "sales_targets",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalToken",
                table: "sales_targets",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FiscalYear",
                table: "sales_targets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "LockedExchangeRate",
                table: "sales_targets",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 1.000000m);

            migrationBuilder.AddColumn<decimal>(
                name: "MarginHurdleTarget",
                table: "sales_targets",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PeriodType",
                table: "sales_targets",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PredecessorTargetId",
                table: "sales_targets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RealizedBilled",
                table: "sales_targets",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RealizedCash",
                table: "sales_targets",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RealizedMarginAmount",
                table: "sales_targets",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RealizedMarginPercentage",
                table: "sales_targets",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RealizedRevenue",
                table: "sales_targets",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RevenueTarget",
                table: "sales_targets",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TargetPeriod",
                table: "sales_targets",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "sales_targets",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "default");

            migrationBuilder.AddColumn<int>(
                name: "TerritoryId",
                table: "sales_targets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VolumeTarget",
                table: "sales_targets",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "sales_targets",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AlterColumn<string>(
                name: "TransactionType",
                table: "sales_target_realizations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AddColumn<int>(
                name: "AdvancePaymentId",
                table: "sales_target_realizations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LockedFXRate",
                table: "sales_target_realizations",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 1.000000m);

            migrationBuilder.AddColumn<decimal>(
                name: "MarginContributionAmount",
                table: "sales_target_realizations",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "QuotationId",
                table: "sales_target_realizations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RealizationType",
                table: "sales_target_realizations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "RealizedQuantity",
                table: "sales_target_realizations",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ReversalOfRealizationId",
                table: "sales_target_realizations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReversalReason",
                table: "sales_target_realizations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SalesInvoiceId",
                table: "sales_target_realizations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SourceAmount",
                table: "sales_target_realizations",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SourceCurrency",
                table: "sales_target_realizations",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "INR");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "sales_target_realizations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "default");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TransactionDate",
                table: "sales_target_realizations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateTable(
                name: "sales_target_approval_histories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "default"),
                    SalesTargetId = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ApproverUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ApproverName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ApproverRole = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    HmacSignatureToken = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_target_approval_histories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sales_target_approval_histories_sales_targets_SalesTargetId",
                        column: x => x.SalesTargetId,
                        principalTable: "sales_targets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sales_target_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "default"),
                    SalesTargetId = table.Column<int>(type: "integer", nullable: false),
                    ProductCategoryId = table.Column<int>(type: "integer", nullable: true),
                    CategoryName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ItemMasterId = table.Column<int>(type: "integer", nullable: true),
                    ItemCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ItemDescription = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TargetQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TargetRate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TargetValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RealizedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    RealizedValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    MinimumMarginPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_target_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sales_target_items_sales_targets_SalesTargetId",
                        column: x => x.SalesTargetId,
                        principalTable: "sales_targets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sales_target_split_rules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "default"),
                    RuleName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    LeadGeneratorPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ClosingRepPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    TechnicalConsultantPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_target_split_rules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sales_targets_branch_fy_perf",
                table: "sales_targets",
                columns: new[] { "TenantId", "Branch", "FiscalYear", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_targets_parent_hierarchy",
                table: "sales_targets",
                columns: new[] { "TenantId", "ParentTargetId", "Status" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_sales_targets_ParentTargetId",
                table: "sales_targets",
                column: "ParentTargetId");

            migrationBuilder.CreateIndex(
                name: "ix_sales_targets_period_rep",
                table: "sales_targets",
                columns: new[] { "TenantId", "Status", "SalesPersonUserId", "StartDate", "EndDate" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_sales_targets_TenantId_TargetNumber",
                table: "sales_targets",
                columns: new[] { "TenantId", "TargetNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_target_realizations_SalesTargetId",
                table: "sales_target_realizations",
                column: "SalesTargetId");

            migrationBuilder.CreateIndex(
                name: "ix_target_realizations_advance_lookup",
                table: "sales_target_realizations",
                columns: new[] { "TenantId", "AdvancePaymentId", "IsReversal" });

            migrationBuilder.CreateIndex(
                name: "ix_target_realizations_order_lookup",
                table: "sales_target_realizations",
                columns: new[] { "TenantId", "SalesOrderId", "IsReversal" });

            migrationBuilder.CreateIndex(
                name: "ix_target_realizations_target_type_date",
                table: "sales_target_realizations",
                columns: new[] { "TenantId", "SalesTargetId", "RealizationType", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_target_approval_histories_SalesTargetId",
                table: "sales_target_approval_histories",
                column: "SalesTargetId");

            migrationBuilder.CreateIndex(
                name: "ix_target_approval_hist_lookup",
                table: "sales_target_approval_histories",
                columns: new[] { "TenantId", "SalesTargetId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_target_items_cat_lookup",
                table: "sales_target_items",
                columns: new[] { "TenantId", "SalesTargetId", "ProductCategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_target_items_SalesTargetId",
                table: "sales_target_items",
                column: "SalesTargetId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_target_split_rules_TenantId_RuleName",
                table: "sales_target_split_rules",
                columns: new[] { "TenantId", "RuleName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_sales_targets_sales_targets_ParentTargetId",
                table: "sales_targets",
                column: "ParentTargetId",
                principalTable: "sales_targets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sales_targets_sales_targets_ParentTargetId",
                table: "sales_targets");

            migrationBuilder.DropTable(
                name: "sales_target_approval_histories");

            migrationBuilder.DropTable(
                name: "sales_target_items");

            migrationBuilder.DropTable(
                name: "sales_target_split_rules");

            migrationBuilder.DropIndex(
                name: "ix_sales_targets_branch_fy_perf",
                table: "sales_targets");

            migrationBuilder.DropIndex(
                name: "ix_sales_targets_parent_hierarchy",
                table: "sales_targets");

            migrationBuilder.DropIndex(
                name: "IX_sales_targets_ParentTargetId",
                table: "sales_targets");

            migrationBuilder.DropIndex(
                name: "ix_sales_targets_period_rep",
                table: "sales_targets");

            migrationBuilder.DropIndex(
                name: "IX_sales_targets_TenantId_TargetNumber",
                table: "sales_targets");

            migrationBuilder.DropIndex(
                name: "IX_sales_target_realizations_SalesTargetId",
                table: "sales_target_realizations");

            migrationBuilder.DropIndex(
                name: "ix_target_realizations_advance_lookup",
                table: "sales_target_realizations");

            migrationBuilder.DropIndex(
                name: "ix_target_realizations_order_lookup",
                table: "sales_target_realizations");

            migrationBuilder.DropIndex(
                name: "ix_target_realizations_target_type_date",
                table: "sales_target_realizations");

            migrationBuilder.DropColumn(
                name: "AchievedVolume",
                table: "sales_targets");

            migrationBuilder.DropColumn(
                name: "ApprovalToken",
                table: "sales_targets");

            migrationBuilder.DropColumn(
                name: "FiscalYear",
                table: "sales_targets");

            migrationBuilder.DropColumn(
                name: "LockedExchangeRate",
                table: "sales_targets");

            migrationBuilder.DropColumn(
                name: "MarginHurdleTarget",
                table: "sales_targets");

            migrationBuilder.DropColumn(
                name: "PeriodType",
                table: "sales_targets");

            migrationBuilder.DropColumn(
                name: "PredecessorTargetId",
                table: "sales_targets");

            migrationBuilder.DropColumn(
                name: "RealizedBilled",
                table: "sales_targets");

            migrationBuilder.DropColumn(
                name: "RealizedCash",
                table: "sales_targets");

            migrationBuilder.DropColumn(
                name: "RealizedMarginAmount",
                table: "sales_targets");

            migrationBuilder.DropColumn(
                name: "RealizedMarginPercentage",
                table: "sales_targets");

            migrationBuilder.DropColumn(
                name: "RealizedRevenue",
                table: "sales_targets");

            migrationBuilder.DropColumn(
                name: "RevenueTarget",
                table: "sales_targets");

            migrationBuilder.DropColumn(
                name: "TargetPeriod",
                table: "sales_targets");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "sales_targets");

            migrationBuilder.DropColumn(
                name: "TerritoryId",
                table: "sales_targets");

            migrationBuilder.DropColumn(
                name: "VolumeTarget",
                table: "sales_targets");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "sales_targets");

            migrationBuilder.DropColumn(
                name: "AdvancePaymentId",
                table: "sales_target_realizations");

            migrationBuilder.DropColumn(
                name: "LockedFXRate",
                table: "sales_target_realizations");

            migrationBuilder.DropColumn(
                name: "MarginContributionAmount",
                table: "sales_target_realizations");

            migrationBuilder.DropColumn(
                name: "QuotationId",
                table: "sales_target_realizations");

            migrationBuilder.DropColumn(
                name: "RealizationType",
                table: "sales_target_realizations");

            migrationBuilder.DropColumn(
                name: "RealizedQuantity",
                table: "sales_target_realizations");

            migrationBuilder.DropColumn(
                name: "ReversalOfRealizationId",
                table: "sales_target_realizations");

            migrationBuilder.DropColumn(
                name: "ReversalReason",
                table: "sales_target_realizations");

            migrationBuilder.DropColumn(
                name: "SalesInvoiceId",
                table: "sales_target_realizations");

            migrationBuilder.DropColumn(
                name: "SourceAmount",
                table: "sales_target_realizations");

            migrationBuilder.DropColumn(
                name: "SourceCurrency",
                table: "sales_target_realizations");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "sales_target_realizations");

            migrationBuilder.DropColumn(
                name: "TransactionDate",
                table: "sales_target_realizations");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "sales_targets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<decimal>(
                name: "TargetValue",
                table: "sales_targets",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "TargetType",
                table: "sales_targets",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "RemainingValue",
                table: "sales_targets",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "OverAchievementValue",
                table: "sales_targets",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "sales_targets",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8)",
                oldMaxLength: 8,
                oldDefaultValue: "INR");

            migrationBuilder.AlterColumn<decimal>(
                name: "AchievementPercentage",
                table: "sales_targets",
                type: "numeric(9,2)",
                precision: 9,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(9,2)",
                oldPrecision: 9,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "AchievedValue",
                table: "sales_targets",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "TransactionType",
                table: "sales_target_realizations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_sales_targets_active_window",
                table: "sales_targets",
                columns: new[] { "Status", "SalesPersonUserId", "TargetCategory", "StartDate", "EndDate", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_targets_FinancialYear",
                table: "sales_targets",
                column: "FinancialYear");

            migrationBuilder.CreateIndex(
                name: "IX_sales_targets_parent_hierarchy",
                table: "sales_targets",
                columns: new[] { "ParentTargetId", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_targets_TargetNumber",
                table: "sales_targets",
                column: "TargetNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_target_realizations_lookup",
                table: "sales_target_realizations",
                columns: new[] { "SalesTargetId", "SalesOrderId", "IsReversal" });

            migrationBuilder.AddForeignKey(
                name: "FK_sales_targets_sales_targets_ParentTargetId",
                table: "sales_targets",
                column: "ParentTargetId",
                principalTable: "sales_targets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
