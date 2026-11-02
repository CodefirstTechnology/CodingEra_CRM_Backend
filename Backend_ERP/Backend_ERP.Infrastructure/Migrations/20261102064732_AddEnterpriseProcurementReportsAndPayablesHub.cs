using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseProcurementReportsAndPayablesHub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE vendor_ledger_entries DROP CONSTRAINT IF EXISTS ""FK_vendor_ledger_entries_purchase_bills_PurchaseBillId"";
                ALTER TABLE vendor_ledger_entries DROP CONSTRAINT IF EXISTS ""FK_vendor_ledger_entries_vendors_VendorId"";
                DROP INDEX IF EXISTS idx_vendor_ledger_lookup;
            ");

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceNumber",
                table: "vendor_ledger_entries",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EntryType",
                table: "vendor_ledger_entries",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<decimal>(
                name: "DebitAmount",
                table: "vendor_ledger_entries",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "CreditAmount",
                table: "vendor_ledger_entries",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "vendor_ledger_entries",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "INR");

            migrationBuilder.AddColumn<string>(
                name: "EntryNumber",
                table: "vendor_ledger_entries",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "vendor_ledger_entries",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 1.000000m);

            migrationBuilder.AddColumn<decimal>(
                name: "ForeignCreditAmount",
                table: "vendor_ledger_entries",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ForeignDebitAmount",
                table: "vendor_ledger_entries",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OutstandingAmount",
                table: "vendor_ledger_entries",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "PostingDate",
                table: "vendor_ledger_entries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "ReferenceDocId",
                table: "vendor_ledger_entries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceDocNumber",
                table: "vendor_ledger_entries",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceDocType",
                table: "vendor_ledger_entries",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RunningBalance",
                table: "vendor_ledger_entries",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SettledAmount",
                table: "vendor_ledger_entries",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SettlementStatus",
                table: "vendor_ledger_entries",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Unsettled");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "vendor_ledger_entries",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "TENANT-DEFAULT");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "vendor_ledger_entries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "vendor_ledger_entries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoucherType",
                table: "vendor_ledger_entries",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "vendor_ledger_entries",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "procurement_audit_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "TENANT-DEFAULT"),
                    BlockIndex = table.Column<long>(type: "bigint", nullable: false),
                    EntityName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActionType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ChangedByUser = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UserIpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    OldValuesJson = table.Column<string>(type: "jsonb", nullable: true),
                    NewValuesJson = table.Column<string>(type: "jsonb", nullable: true),
                    PreviousLogHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CurrentLogHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_procurement_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "procurement_workflow_stage_logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "TENANT-DEFAULT"),
                    EntityType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EntityId = table.Column<int>(type: "integer", nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StageSequence = table.Column<int>(type: "integer", nullable: false),
                    StageName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ElapsedBusinessHours = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    TargetSlaHours = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    IsSlaBreached = table.Column<bool>(type: "boolean", nullable: false),
                    DepartmentName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ActionedByUserId = table.Column<int>(type: "integer", nullable: true),
                    ActionedByUserName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Remarks = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_procurement_workflow_stage_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vendor_bill_settlements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "TENANT-DEFAULT"),
                    PurchaseBillId = table.Column<int>(type: "integer", nullable: false),
                    PaymentVoucherId = table.Column<int>(type: "integer", nullable: true),
                    DebitNoteId = table.Column<int>(type: "integer", nullable: true),
                    AllocatedAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    SettlementDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RealizedFxGainLoss = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    AllocationNotes = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_bill_settlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vendor_bill_settlements_purchase_bills_PurchaseBillId",
                        column: x => x.PurchaseBillId,
                        principalTable: "purchase_bills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_bill_settlements_vendor_payments_PaymentVoucherId",
                        column: x => x.PaymentVoucherId,
                        principalTable: "vendor_payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_vendor_ledger_unsettled",
                table: "vendor_ledger_entries",
                columns: new[] { "TenantId", "VendorId", "SettlementStatus" });

            migrationBuilder.CreateIndex(
                name: "idx_vendor_ledger_vendor_date",
                table: "vendor_ledger_entries",
                columns: new[] { "TenantId", "VendorId", "PostingDate", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_vendor_ledger_entries_VendorId",
                table: "vendor_ledger_entries",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_bill_lines_PurchaseOrderLineId",
                table: "purchase_bill_lines",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "idx_procurement_audit_chain",
                table: "procurement_audit_logs",
                columns: new[] { "TenantId", "BlockIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_procurement_audit_entity",
                table: "procurement_audit_logs",
                columns: new[] { "TenantId", "EntityName", "EntityId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "idx_procurement_stage_perf",
                table: "procurement_workflow_stage_logs",
                columns: new[] { "TenantId", "EntityType", "IsSlaBreached", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "idx_bill_settlement_lookup",
                table: "vendor_bill_settlements",
                columns: new[] { "TenantId", "PurchaseBillId", "PaymentVoucherId" });

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bill_settlements_PaymentVoucherId",
                table: "vendor_bill_settlements",
                column: "PaymentVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bill_settlements_PurchaseBillId",
                table: "vendor_bill_settlements",
                column: "PurchaseBillId");

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_bill_lines_purchase_order_lines_PurchaseOrderLineId",
                table: "purchase_bill_lines",
                column: "PurchaseOrderLineId",
                principalTable: "purchase_order_lines",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_vendor_ledger_entries_purchase_bills_PurchaseBillId",
                table: "vendor_ledger_entries",
                column: "PurchaseBillId",
                principalTable: "purchase_bills",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_vendor_ledger_entries_vendors_VendorId",
                table: "vendor_ledger_entries",
                column: "VendorId",
                principalTable: "vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Idempotent Check Constraints
            migrationBuilder.Sql(@"
                DO $body$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_vendor_ledger_amounts') THEN
                        ALTER TABLE vendor_ledger_entries 
                        ADD CONSTRAINT chk_vendor_ledger_amounts 
                        CHECK (""DebitAmount"" >= 0 AND ""CreditAmount"" >= 0);
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_settlement_allocated_positive') THEN
                        ALTER TABLE vendor_bill_settlements 
                        ADD CONSTRAINT chk_settlement_allocated_positive 
                        CHECK (""AllocatedAmount"" > 0);
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_stage_elapsed_positive') THEN
                        ALTER TABLE procurement_workflow_stage_logs 
                        ADD CONSTRAINT chk_stage_elapsed_positive 
                        CHECK (""ElapsedBusinessHours"" IS NULL OR ""ElapsedBusinessHours"" >= 0);
                    END IF;
                END $body$;
            ");

            // Idempotent PostgreSQL Running Balance Trigger Function with Recursion Guard
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION fn_trg_sync_vendor_ledger_running_balance()
                RETURNS TRIGGER AS $body$
                DECLARE
                    v_running_bal NUMERIC(18,4) := 0;
                    v_target_vendor_id INT;
                    v_entry RECORD;
                BEGIN
                    -- Prevent recursive trigger loop during the UPDATE statement below
                    IF pg_trigger_depth() > 1 THEN
                        RETURN NEW;
                    END IF;

                    v_target_vendor_id := COALESCE(NEW.""VendorId"", OLD.""VendorId"");

                    FOR v_entry IN
                        SELECT ""Id"", ""DebitAmount"", ""CreditAmount""
                        FROM vendor_ledger_entries
                        WHERE ""VendorId"" = v_target_vendor_id
                        ORDER BY COALESCE(""PostingDate"", ""EntryDate"") ASC, ""Id"" ASC
                    LOOP
                        v_running_bal := v_running_bal + (v_entry.""CreditAmount"" - v_entry.""DebitAmount"");
                        UPDATE vendor_ledger_entries
                        SET ""RunningBalance"" = v_running_bal
                        WHERE ""Id"" = v_entry.""Id"";
                    END LOOP;

                    RETURN NEW;
                END;
                $body$ LANGUAGE plpgsql;

                DROP TRIGGER IF EXISTS trg_sync_vendor_ledger_running_balance ON vendor_ledger_entries;
                CREATE TRIGGER trg_sync_vendor_ledger_running_balance
                AFTER INSERT OR DELETE ON vendor_ledger_entries
                FOR EACH ROW
                EXECUTE FUNCTION fn_trg_sync_vendor_ledger_running_balance();
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_purchase_bill_lines_purchase_order_lines_PurchaseOrderLineId",
                table: "purchase_bill_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_vendor_ledger_entries_purchase_bills_PurchaseBillId",
                table: "vendor_ledger_entries");

            migrationBuilder.DropForeignKey(
                name: "FK_vendor_ledger_entries_vendors_VendorId",
                table: "vendor_ledger_entries");

            migrationBuilder.DropTable(
                name: "procurement_audit_logs");

            migrationBuilder.DropTable(
                name: "procurement_workflow_stage_logs");

            migrationBuilder.DropTable(
                name: "vendor_bill_settlements");

            migrationBuilder.DropIndex(
                name: "idx_vendor_ledger_unsettled",
                table: "vendor_ledger_entries");

            migrationBuilder.DropIndex(
                name: "idx_vendor_ledger_vendor_date",
                table: "vendor_ledger_entries");

            migrationBuilder.DropIndex(
                name: "IX_vendor_ledger_entries_VendorId",
                table: "vendor_ledger_entries");

            migrationBuilder.DropIndex(
                name: "IX_purchase_bill_lines_PurchaseOrderLineId",
                table: "purchase_bill_lines");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "vendor_ledger_entries");

            migrationBuilder.DropColumn(
                name: "EntryNumber",
                table: "vendor_ledger_entries");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "vendor_ledger_entries");

            migrationBuilder.DropColumn(
                name: "ForeignCreditAmount",
                table: "vendor_ledger_entries");

            migrationBuilder.DropColumn(
                name: "ForeignDebitAmount",
                table: "vendor_ledger_entries");

            migrationBuilder.DropColumn(
                name: "OutstandingAmount",
                table: "vendor_ledger_entries");

            migrationBuilder.DropColumn(
                name: "PostingDate",
                table: "vendor_ledger_entries");

            migrationBuilder.DropColumn(
                name: "ReferenceDocId",
                table: "vendor_ledger_entries");

            migrationBuilder.DropColumn(
                name: "ReferenceDocNumber",
                table: "vendor_ledger_entries");

            migrationBuilder.DropColumn(
                name: "ReferenceDocType",
                table: "vendor_ledger_entries");

            migrationBuilder.DropColumn(
                name: "RunningBalance",
                table: "vendor_ledger_entries");

            migrationBuilder.DropColumn(
                name: "SettledAmount",
                table: "vendor_ledger_entries");

            migrationBuilder.DropColumn(
                name: "SettlementStatus",
                table: "vendor_ledger_entries");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "vendor_ledger_entries");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "vendor_ledger_entries");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "vendor_ledger_entries");

            migrationBuilder.DropColumn(
                name: "VoucherType",
                table: "vendor_ledger_entries");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "vendor_ledger_entries");

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceNumber",
                table: "vendor_ledger_entries",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EntryType",
                table: "vendor_ledger_entries",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "DebitAmount",
                table: "vendor_ledger_entries",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "CreditAmount",
                table: "vendor_ledger_entries",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "idx_vendor_ledger_lookup",
                table: "vendor_ledger_entries",
                columns: new[] { "VendorId", "EntryDate", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_vendor_ledger_entries_purchase_bills_PurchaseBillId",
                table: "vendor_ledger_entries",
                column: "PurchaseBillId",
                principalTable: "purchase_bills",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_vendor_ledger_entries_vendors_VendorId",
                table: "vendor_ledger_entries",
                column: "VendorId",
                principalTable: "vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
