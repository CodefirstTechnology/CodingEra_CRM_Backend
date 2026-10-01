using System;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <summary>
    /// Migration: AddProductionShopFloorExecution
    /// Adds tables for Shop-Floor Production Execution (Group 3):
    /// 1. shop_floor_production_entries
    /// 2. material_consumption_logs
    /// 3. rejection_tracking_logs
    /// 4. daily_production_reports
    /// 5. daily_production_report_entries
    /// </summary>
    [DbContext(typeof(ERPDbContext))]
    [Migration("20261001120000_AddProductionShopFloorExecution")]
    public partial class AddProductionShopFloorExecution : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. SHOP FLOOR PRODUCTION ENTRIES
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS shop_floor_production_entries (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    entry_number VARCHAR(50) NOT NULL UNIQUE,
                    work_order_operation_id UUID NOT NULL REFERENCES work_order_operations(id) ON DELETE RESTRICT,
                    shift_id VARCHAR(50) NOT NULL,
                    operator_id VARCHAR(50) NOT NULL,
                    machine_id UUID NOT NULL,
                    good_quantity NUMERIC(18, 4) NOT NULL CHECK (good_quantity >= 0),
                    scrapped_quantity NUMERIC(18, 4) NOT NULL CHECK (scrapped_quantity >= 0),
                    start_time TIMESTAMPTZ NOT NULL,
                    end_time TIMESTAMPTZ NOT NULL,
                    status VARCHAR(30) NOT NULL DEFAULT 'SUBMITTED' CHECK (status IN ('DRAFT', 'SUBMITTED', 'APPROVED', 'REJECTED')),
                    supervisor_approved_by VARCHAR(100) NULL,
                    supervisor_approved_at TIMESTAMPTZ NULL,
                    version_token INT NOT NULL DEFAULT 1,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
                );
            ");

            // 2. MATERIAL CONSUMPTION LOGS
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS material_consumption_logs (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    production_entry_id UUID NOT NULL REFERENCES shop_floor_production_entries(id) ON DELETE CASCADE,
                    raw_material_item_id UUID NOT NULL,
                    batch_id UUID NULL,
                    standard_bom_quantity NUMERIC(18, 6) NOT NULL CHECK (standard_bom_quantity >= 0),
                    actual_consumed_quantity NUMERIC(18, 6) NOT NULL CHECK (actual_consumed_quantity >= 0),
                    variance_quantity NUMERIC(18, 6) NOT NULL,
                    consumption_type VARCHAR(30) NOT NULL CHECK (consumption_type IN ('BACKFLUSH', 'MANUAL')),
                    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
                );
            ");

            // 3. REJECTION TRACKING LOGS
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS rejection_tracking_logs (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    production_entry_id UUID NOT NULL REFERENCES shop_floor_production_entries(id) ON DELETE CASCADE,
                    work_order_id UUID NOT NULL REFERENCES work_orders(id) ON DELETE RESTRICT,
                    defect_category VARCHAR(30) NOT NULL CHECK (defect_category IN ('DIMENSIONAL', 'SURFACE', 'MATERIAL_FLAW', 'OPERATOR_ERROR', 'SETUP_REJECT')),
                    defect_reason_code VARCHAR(50) NOT NULL,
                    rejected_quantity NUMERIC(18, 4) NOT NULL CHECK (rejected_quantity > 0),
                    disposition VARCHAR(30) NOT NULL CHECK (disposition IN ('SCRAP', 'REWORK', 'VENDOR_RETURN')),
                    unit_scrap_cost NUMERIC(18, 4) NOT NULL DEFAULT 0.0000,
                    total_loss_cost NUMERIC(18, 4) NOT NULL DEFAULT 0.0000,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
                );
            ");

            // 4. DAILY PRODUCTION REPORTS
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS daily_production_reports (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    report_date DATE NOT NULL,
                    shift_id VARCHAR(50) NOT NULL,
                    work_center_id UUID NOT NULL,
                    total_planned_qty NUMERIC(18, 4) NOT NULL DEFAULT 0.0000,
                    total_good_qty NUMERIC(18, 4) NOT NULL DEFAULT 0.0000,
                    total_scrap_qty NUMERIC(18, 4) NOT NULL DEFAULT 0.0000,
                    total_downtime_minutes NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
                    oee_percentage NUMERIC(5, 2) NOT NULL DEFAULT 0.00,
                    availability_percentage NUMERIC(5, 2) NOT NULL DEFAULT 0.00,
                    performance_percentage NUMERIC(5, 2) NOT NULL DEFAULT 0.00,
                    quality_percentage NUMERIC(5, 2) NOT NULL DEFAULT 0.00,
                    is_finalized BOOLEAN NOT NULL DEFAULT FALSE,
                    finalized_by VARCHAR(100) NULL,
                    finalized_at TIMESTAMPTZ NULL,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMPTZ NULL,
                    CONSTRAINT uq_dpr_date_shift_workcenter UNIQUE (report_date, shift_id, work_center_id)
                );
            ");

            // 5. DAILY PRODUCTION REPORT ENTRIES (JUNCTION TABLE)
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS daily_production_report_entries (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    daily_production_report_id UUID NOT NULL REFERENCES daily_production_reports(id) ON DELETE CASCADE,
                    production_entry_id UUID NOT NULL REFERENCES shop_floor_production_entries(id) ON DELETE RESTRICT,
                    linked_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT uq_dpr_entry_link UNIQUE (daily_production_report_id, production_entry_id)
                );
            ");

            // NON-BLOCKING INDEXES
            migrationBuilder.Sql("CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_prod_entries_wo_op ON shop_floor_production_entries (work_order_operation_id, status);", suppressTransaction: true);
            migrationBuilder.Sql("CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_mat_consumption_batch ON material_consumption_logs (raw_material_item_id, batch_id);", suppressTransaction: true);
            migrationBuilder.Sql("CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_rejection_wo ON rejection_tracking_logs (work_order_id, defect_category);", suppressTransaction: true);
            migrationBuilder.Sql("CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_dpr_date_wc ON daily_production_reports (report_date, work_center_id);", suppressTransaction: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS daily_production_report_entries CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS daily_production_reports CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS rejection_tracking_logs CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS material_consumption_logs CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS shop_floor_production_entries CASCADE;");
        }
    }
}
