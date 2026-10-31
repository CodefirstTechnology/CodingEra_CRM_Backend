using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseVendorsHubNexus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_vendors_GSTIN",
                table: "vendors");

            migrationBuilder.DropIndex(
                name: "IX_vendors_Name",
                table: "vendors");

            migrationBuilder.DropIndex(
                name: "IX_vendors_PAN",
                table: "vendors");

            migrationBuilder.DropIndex(
                name: "IX_vendors_Status",
                table: "vendors");

            migrationBuilder.DropIndex(
                name: "IX_vendors_VendorCode",
                table: "vendors");

            migrationBuilder.AddColumn<string>(
                name: "ApprovalToken",
                table: "vendors",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cin",
                table: "vendors",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "vendors",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "INR");

            migrationBuilder.AddColumn<string>(
                name: "Incoterms",
                table: "vendors",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsKycVerified",
                table: "vendors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsMsmeRegistered",
                table: "vendors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MsmeCategory",
                table: "vendors",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MsmeRegistrationNumber",
                table: "vendors",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentTermsDays",
                table: "vendors",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<decimal>(
                name: "RatingScore",
                table: "vendors",
                type: "numeric(7,2)",
                precision: 7,
                scale: 2,
                nullable: false,
                defaultValue: 100.00m);

            migrationBuilder.AddColumn<string>(
                name: "Tan",
                table: "vendors",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "vendors",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "default");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalOpenCommitment",
                table: "vendors",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0.00m);

            migrationBuilder.AddColumn<string>(
                name: "TradeName",
                table: "vendors",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VendorType",
                table: "vendors",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "vendors",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "vendor_contacts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Mobile",
                table: "vendor_contacts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "vendor_contacts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "default");

            migrationBuilder.CreateTable(
                name: "vendor_approval_histories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "default"),
                    VendorId = table.Column<int>(type: "integer", nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NewStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Comments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ActionedByUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActionedByUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ApprovalToken = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ActionedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_approval_histories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vendor_approval_histories_vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vendor_bank_accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "default"),
                    VendorId = table.Column<int>(type: "integer", nullable: false),
                    AccountNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IfscCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BankName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Branch = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    BeneficiaryName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AccountType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Current"),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    VerificationStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CancelledChequeUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerifiedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_bank_accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vendor_bank_accounts_vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vendor_item_catalogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "default"),
                    VendorId = table.Column<int>(type: "integer", nullable: false),
                    ItemType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    ItemCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SupplierPartNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    StandardLeadTimeDays = table.Column<int>(type: "integer", nullable: false),
                    MinimumOrderQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 1.0000m),
                    OrderMultiple = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 1.0000m),
                    PackagingUnit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BaseUomConversionMultiplier = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false, defaultValue: 1.000000m),
                    UnitPriceSnapshot = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "INR"),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    TechnicalRemarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_item_catalogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vendor_item_catalogs_vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vendor_performance_scores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "default"),
                    VendorId = table.Column<int>(type: "integer", nullable: false),
                    PeriodKey = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FiscalYear = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    OtifPercentage = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    QualityPassRate = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    PriceVarianceScore = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    ComplianceScore = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    BlendedRatingScore = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    TotalLotsReceived = table.Column<int>(type: "integer", nullable: false),
                    TotalLotsRejected = table.Column<int>(type: "integer", nullable: false),
                    DefectPartsPerMillion = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_performance_scores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vendor_performance_scores_vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_vendors_tenant_code",
                table: "vendors",
                columns: new[] { "TenantId", "VendorCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vendors_tenant_gstin",
                table: "vendors",
                columns: new[] { "TenantId", "GSTIN" });

            migrationBuilder.CreateIndex(
                name: "ix_vendors_tenant_msme_compliance",
                table: "vendors",
                columns: new[] { "TenantId", "MsmeCategory", "IsMsmeRegistered" });

            migrationBuilder.CreateIndex(
                name: "ix_vendors_tenant_pan",
                table: "vendors",
                columns: new[] { "TenantId", "PAN" });

            migrationBuilder.CreateIndex(
                name: "ix_vendors_tenant_status_rating",
                table: "vendors",
                columns: new[] { "TenantId", "Status", "RatingScore" });

            migrationBuilder.CreateIndex(
                name: "IX_vendor_contacts_TenantId_VendorId",
                table: "vendor_contacts",
                columns: new[] { "TenantId", "VendorId" });

            migrationBuilder.CreateIndex(
                name: "IX_vendor_approval_histories_VendorId",
                table: "vendor_approval_histories",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_approvals_tenant_vendor_date",
                table: "vendor_approval_histories",
                columns: new[] { "TenantId", "VendorId", "ActionedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bank_accounts_VendorId",
                table: "vendor_bank_accounts",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_banks_tenant_vendor_account",
                table: "vendor_bank_accounts",
                columns: new[] { "TenantId", "VendorId", "AccountNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vendor_catalog_tenant_avl_lookup",
                table: "vendor_item_catalogs",
                columns: new[] { "TenantId", "ItemCode", "IsApproved" });

            migrationBuilder.CreateIndex(
                name: "ix_vendor_catalog_tenant_vendor_item_effective",
                table: "vendor_item_catalogs",
                columns: new[] { "TenantId", "VendorId", "ItemCode", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_item_catalogs_VendorId",
                table: "vendor_item_catalogs",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_perf_tenant_vendor_period",
                table: "vendor_performance_scores",
                columns: new[] { "TenantId", "VendorId", "PeriodKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_performance_scores_VendorId",
                table: "vendor_performance_scores",
                column: "VendorId");

            migrationBuilder.Sql(@"
DO $$
BEGIN
    -- 1. Check constraints
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'chk_vendor_rating_bounds'
    ) THEN
        ALTER TABLE vendors ADD CONSTRAINT chk_vendor_rating_bounds CHECK (""RatingScore"" >= 0.00 AND ""RatingScore"" <= 100.00);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'chk_vendor_catalog_moq_positive'
    ) THEN
        ALTER TABLE vendor_item_catalogs ADD CONSTRAINT chk_vendor_catalog_moq_positive CHECK (""MinimumOrderQuantity"" > 0);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'chk_vendor_catalog_multiplier_positive'
    ) THEN
        ALTER TABLE vendor_item_catalogs ADD CONSTRAINT chk_vendor_catalog_multiplier_positive CHECK (""BaseUomConversionMultiplier"" > 0);
    END IF;

    -- 2. Trigger function to synchronize vendor rating score atomically
    CREATE OR REPLACE FUNCTION fn_trg_sync_vendor_rating_score()
    RETURNS TRIGGER AS $trg$
    BEGIN
        UPDATE vendors
        SET ""RatingScore"" = NEW.""BlendedRatingScore"",
            ""UpdatedAt"" = NOW()
        WHERE ""Id"" = NEW.""VendorId"";
        RETURN NEW;
    END;
    $trg$ LANGUAGE plpgsql;

    -- 3. Bind trigger on vendor_performance_scores
    IF NOT EXISTS (
        SELECT 1 FROM pg_trigger WHERE tgname = 'trg_sync_vendor_rating_score'
    ) THEN
        CREATE TRIGGER trg_sync_vendor_rating_score
        AFTER INSERT OR UPDATE OF ""BlendedRatingScore""
        ON vendor_performance_scores
        FOR EACH ROW
        EXECUTE FUNCTION fn_trg_sync_vendor_rating_score();
    END IF;

    -- 4. Trigram index for vendor directory fuzzy search (if pg_trgm available)
    BEGIN
        CREATE EXTENSION IF NOT EXISTS pg_trgm;
        IF NOT EXISTS (
            SELECT 1 FROM pg_indexes WHERE indexname = 'ix_vendors_trgm_name'
        ) THEN
            CREATE INDEX ix_vendors_trgm_name ON vendors USING gin (""Name"" gin_trgm_ops);
        END IF;
    EXCEPTION WHEN OTHERS THEN
        RAISE NOTICE 'pg_trgm extension or index creation bypassed: %', SQLERRM;
    END;
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vendor_approval_histories");

            migrationBuilder.DropTable(
                name: "vendor_bank_accounts");

            migrationBuilder.DropTable(
                name: "vendor_item_catalogs");

            migrationBuilder.DropTable(
                name: "vendor_performance_scores");

            migrationBuilder.DropIndex(
                name: "ix_vendors_tenant_code",
                table: "vendors");

            migrationBuilder.DropIndex(
                name: "ix_vendors_tenant_gstin",
                table: "vendors");

            migrationBuilder.DropIndex(
                name: "ix_vendors_tenant_msme_compliance",
                table: "vendors");

            migrationBuilder.DropIndex(
                name: "ix_vendors_tenant_pan",
                table: "vendors");

            migrationBuilder.DropIndex(
                name: "ix_vendors_tenant_status_rating",
                table: "vendors");

            migrationBuilder.DropIndex(
                name: "IX_vendor_contacts_TenantId_VendorId",
                table: "vendor_contacts");

            migrationBuilder.DropColumn(
                name: "ApprovalToken",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "Cin",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "Incoterms",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "IsKycVerified",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "IsMsmeRegistered",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "MsmeCategory",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "MsmeRegistrationNumber",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "PaymentTermsDays",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "RatingScore",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "Tan",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "TotalOpenCommitment",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "TradeName",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "VendorType",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "vendor_contacts");

            migrationBuilder.DropColumn(
                name: "Mobile",
                table: "vendor_contacts");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "vendor_contacts");

            migrationBuilder.CreateIndex(
                name: "IX_vendors_GSTIN",
                table: "vendors",
                column: "GSTIN");

            migrationBuilder.CreateIndex(
                name: "IX_vendors_Name",
                table: "vendors",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_vendors_PAN",
                table: "vendors",
                column: "PAN");

            migrationBuilder.CreateIndex(
                name: "IX_vendors_Status",
                table: "vendors",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_vendors_VendorCode",
                table: "vendors",
                column: "VendorCode",
                unique: true);

            migrationBuilder.Sql(@"
DO $$
BEGIN
    DROP TRIGGER IF EXISTS trg_sync_vendor_rating_score ON vendor_performance_scores;
    DROP FUNCTION IF EXISTS fn_trg_sync_vendor_rating_score();
    DROP INDEX IF EXISTS ix_vendors_trgm_name;
    ALTER TABLE vendors DROP CONSTRAINT IF EXISTS chk_vendor_rating_bounds;
    ALTER TABLE vendor_item_catalogs DROP CONSTRAINT IF EXISTS chk_vendor_catalog_moq_positive;
    ALTER TABLE vendor_item_catalogs DROP CONSTRAINT IF EXISTS chk_vendor_catalog_multiplier_positive;
END $$;
");
        }
    }
}
