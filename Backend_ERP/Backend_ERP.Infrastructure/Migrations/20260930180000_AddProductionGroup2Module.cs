using System;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <summary>
    /// Production Migration: AddProductionGroup2Module
    /// Adds tables and constraints for ERP Production Module Group 2:
    /// 1. work_orders
    /// 2. work_order_operations
    /// 3. work_order_material_reservations
    /// 4. production_schedules
    /// 5. machine_schedules (with PostgreSQL btree_gist tsrange Exclusion Constraint)
    /// 6. machine_downtime_logs
    /// </summary>
    [DbContext(typeof(ERPDbContext))]
    [Migration("20260930180000_AddProductionGroup2Module")]
    public partial class AddProductionGroup2Module : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable btree_gist extension for PostgreSQL tsrange exclusion constraints
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            // Drop legacy stub tables if present to align schema with UUID primary keys
            migrationBuilder.Sql("DROP TABLE IF EXISTS machine_downtime_logs CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS machine_schedules CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS production_schedules CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS work_order_material_reservations CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS work_order_operations CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS work_orders CASCADE;");

            // 1. WORK ORDERS
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS work_orders (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    work_order_number VARCHAR(50) NOT NULL UNIQUE,
                    parent_work_order_id UUID NULL REFERENCES work_orders(id) ON DELETE RESTRICT,
                    production_plan_item_id UUID NOT NULL REFERENCES production_plan_items(id) ON DELETE RESTRICT,
                    finished_good_item_id UUID NOT NULL,
                    bom_revision_id UUID NOT NULL REFERENCES bom_revisions(id) ON DELETE RESTRICT,
                    target_quantity NUMERIC(18, 4) NOT NULL CHECK (target_quantity > 0),
                    completed_quantity NUMERIC(18, 4) NOT NULL DEFAULT 0.0000,
                    scrapped_quantity NUMERIC(18, 4) NOT NULL DEFAULT 0.0000,
                    status VARCHAR(30) NOT NULL DEFAULT 'DRAFT' CHECK (status IN ('DRAFT', 'SCHEDULED', 'RELEASED', 'IN_PROGRESS', 'QUALITY_HOLD', 'COMPLETED', 'CLOSED', 'CANCELLED')),
                    priority INT NOT NULL DEFAULT 1,
                    scheduled_start_date TIMESTAMPTZ NULL,
                    scheduled_end_date TIMESTAMPTZ NULL,
                    actual_start_date TIMESTAMPTZ NULL,
                    actual_end_date TIMESTAMPTZ NULL,
                    routing_snapshot_json JSONB NOT NULL DEFAULT '{}'::jsonb,
                    created_by VARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
                    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    updated_by VARCHAR(100) NULL,
                    updated_at TIMESTAMPTZ NULL,
                    version_token INT NOT NULL DEFAULT 1
                );
            ");

            // 2. WORK ORDER OPERATIONS
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS work_order_operations (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    work_order_id UUID NOT NULL REFERENCES work_orders(id) ON DELETE CASCADE,
                    operation_sequence INT NOT NULL,
                    operation_name VARCHAR(100) NOT NULL,
                    work_center_id UUID NOT NULL,
                    assigned_machine_id UUID NULL,
                    setup_time_minutes NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
                    run_time_seconds_per_unit NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
                    labor_hours_per_unit NUMERIC(10, 4) NOT NULL DEFAULT 0.0000,
                    actual_setup_minutes NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
                    actual_run_minutes NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
                    produced_quantity NUMERIC(18, 4) NOT NULL DEFAULT 0.0000,
                    scrapped_quantity NUMERIC(18, 4) NOT NULL DEFAULT 0.0000,
                    requires_qc_inspection BOOLEAN NOT NULL DEFAULT FALSE,
                    status VARCHAR(30) NOT NULL DEFAULT 'PENDING' CHECK (status IN ('PENDING', 'SCHEDULED', 'IN_PROGRESS', 'QUALITY_HOLD', 'COMPLETED', 'SKIPPED')),
                    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT uq_wo_operation_seq UNIQUE (work_order_id, operation_sequence)
                );
            ");

            // 3. WORK ORDER MATERIAL RESERVATIONS
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS work_order_material_reservations (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    work_order_id UUID NOT NULL REFERENCES work_orders(id) ON DELETE CASCADE,
                    raw_material_item_id UUID NOT NULL,
                    warehouse_id UUID NOT NULL,
                    staging_bin_id UUID NULL,
                    batch_id UUID NULL,
                    required_quantity NUMERIC(18, 6) NOT NULL CHECK (required_quantity > 0),
                    staged_quantity NUMERIC(18, 6) NOT NULL DEFAULT 0.000000,
                    issued_quantity NUMERIC(18, 6) NOT NULL DEFAULT 0.000000,
                    status VARCHAR(30) NOT NULL DEFAULT 'RESERVED' CHECK (status IN ('RESERVED', 'STAGED', 'ISSUED', 'RETURNED')),
                    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMPTZ NULL
                );
            ");

            // 4. PRODUCTION SCHEDULES
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS production_schedules (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    schedule_code VARCHAR(50) NOT NULL UNIQUE,
                    schedule_name VARCHAR(150) NOT NULL,
                    start_date DATE NOT NULL,
                    end_date DATE NOT NULL,
                    is_active BOOLEAN NOT NULL DEFAULT TRUE,
                    is_frozen BOOLEAN NOT NULL DEFAULT FALSE,
                    created_by VARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
                    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMPTZ NULL
                );
            ");

            // 5. MACHINE SCHEDULES (WITH EXCLUSION CONSTRAINT)
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS machine_schedules (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    production_schedule_id UUID NOT NULL REFERENCES production_schedules(id) ON DELETE CASCADE,
                    machine_id UUID NOT NULL,
                    work_order_operation_id UUID NULL REFERENCES work_order_operations(id) ON DELETE SET NULL,
                    maintenance_order_id UUID NULL,
                    booking_type VARCHAR(30) NOT NULL DEFAULT 'PRODUCTION' CHECK (booking_type IN ('PRODUCTION', 'MAINTENANCE_BLOCKED', 'CHANGEOVER', 'SHIFT_HOLIDAY')),
                    schedule_range TSRANGE NOT NULL,
                    status VARCHAR(30) NOT NULL DEFAULT 'BOOKED' CHECK (status IN ('BOOKED', 'IN_PROGRESS', 'COMPLETED', 'CANCELLED', 'OVERRUN')),
                    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT ex_no_overlapping_machine_slots EXCLUDE USING gist (
                        machine_id WITH =,
                        schedule_range WITH &&
                    ) WHERE (status IN ('BOOKED', 'IN_PROGRESS'))
                );
            ");

            // 6. MACHINE DOWNTIME LOGS
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS machine_downtime_logs (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    machine_id UUID NOT NULL,
                    work_order_operation_id UUID NULL REFERENCES work_order_operations(id) ON DELETE SET NULL,
                    downtime_category VARCHAR(30) NOT NULL CHECK (downtime_category IN ('UNPLANNED_BREAKDOWN', 'RAW_MATERIAL_WAIT', 'QC_WAIT', 'OPERATOR_ABSENT', 'PLANNED_PPM', 'CHANGEOVER', 'MICRO_STOPPAGE')),
                    start_timestamp TIMESTAMPTZ NOT NULL,
                    end_timestamp TIMESTAMPTZ NULL,
                    reason_code VARCHAR(50) NOT NULL,
                    operator_remarks TEXT NULL,
                    reported_by VARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
                    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
                );
            ");

            // NON-BLOCKING CONCURRENT INDEXES
            migrationBuilder.Sql("CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_work_orders_status ON work_orders (status, scheduled_start_date);", suppressTransaction: true);
            migrationBuilder.Sql("CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_wo_operations_work_center ON work_order_operations (work_center_id, status);", suppressTransaction: true);
            migrationBuilder.Sql("CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_wo_material_res_batch ON work_order_material_reservations (raw_material_item_id, batch_id);", suppressTransaction: true);
            migrationBuilder.Sql("CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_machine_schedules_range ON machine_schedules USING gist (schedule_range);", suppressTransaction: true);
            migrationBuilder.Sql("CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_downtime_logs_machine ON machine_downtime_logs (machine_id, start_timestamp DESC);", suppressTransaction: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS machine_downtime_logs CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS machine_schedules CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS production_schedules CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS work_order_material_reservations CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS work_order_operations CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS work_orders CASCADE;");
        }
    }
}
