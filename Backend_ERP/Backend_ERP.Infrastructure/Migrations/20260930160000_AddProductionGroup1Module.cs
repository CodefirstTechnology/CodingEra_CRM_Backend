using System;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <summary>
    /// Production Migration: AddProductionGroup1Module
    /// Adds tables and constraints for ERP Production Module Group 1:
    /// 1. production_hub_metrics_cache
    /// 2. boms (Header)
    /// 3. bom_revisions
    /// 4. bom_operations (Routings)
    /// 5. bom_items (Components & Scrap Factors)
    /// 6. production_plans (Header)
    /// 7. production_plan_items
    /// 8. production_plan_material_allocations
    /// Includes non-blocking concurrent indexes, PostgreSQL xmin concurrency tokens, and FK safety checks.
    /// </summary>
    [DbContext(typeof(ERPDbContext))]
    [Migration("20260930160000_AddProductionGroup1Module")]
    public partial class AddProductionGroup1Module : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop legacy stub tables if present to align schema with UUID primary keys
            migrationBuilder.Sql("DROP TABLE IF EXISTS production_plans CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS boms CASCADE;");

            // 1. PRODUCTION HUB METRICS CACHE
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS production_hub_metrics_cache (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    snapshot_timestamp TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    work_center_id UUID NULL,
                    oee_percentage NUMERIC(5, 2) NOT NULL DEFAULT 0.00,
                    availability_percentage NUMERIC(5, 2) NOT NULL DEFAULT 0.00,
                    performance_percentage NUMERIC(5, 2) NOT NULL DEFAULT 0.00,
                    quality_percentage NUMERIC(5, 2) NOT NULL DEFAULT 0.00,
                    active_work_orders_count INT NOT NULL DEFAULT 0,
                    bottleneck_risk_level VARCHAR(20) NOT NULL DEFAULT 'NONE' CHECK (bottleneck_risk_level IN ('NONE', 'LOW', 'MEDIUM', 'HIGH', 'CRITICAL')),
                    metrics_metadata JSONB NULL,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
                );
            ");

            // 2. BOMS (HEADER)
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS boms (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    bom_code VARCHAR(50) NOT NULL UNIQUE,
                    finished_good_item_id UUID NOT NULL,
                    description VARCHAR(255) NOT NULL,
                    is_phantom BOOLEAN NOT NULL DEFAULT FALSE,
                    allow_sub_assemblies BOOLEAN NOT NULL DEFAULT TRUE,
                    created_by VARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
                    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    updated_by VARCHAR(100) NULL,
                    updated_at TIMESTAMPTZ NULL,
                    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
                    version_token INT NOT NULL DEFAULT 1
                );
            ");

            // 3. BOM REVISIONS
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS bom_revisions (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    bom_id UUID NOT NULL REFERENCES boms(id) ON DELETE RESTRICT,
                    revision_code VARCHAR(20) NOT NULL,
                    status VARCHAR(30) NOT NULL DEFAULT 'DRAFT' CHECK (status IN ('DRAFT', 'PENDING_APPROVAL', 'APPROVED', 'RELEASED', 'OBSOLETE')),
                    effective_start_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    effective_end_date TIMESTAMPTZ NULL,
                    eco_number VARCHAR(50) NULL,
                    eco_notes TEXT NULL,
                    approved_by VARCHAR(100) NULL,
                    approved_at TIMESTAMPTZ NULL,
                    created_by VARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
                    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    updated_by VARCHAR(100) NULL,
                    updated_at TIMESTAMPTZ NULL,
                    CONSTRAINT uq_bom_revision UNIQUE (bom_id, revision_code)
                );
            ");

            // 4. BOM OPERATIONS / ROUTINGS
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS bom_operations (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    bom_revision_id UUID NOT NULL REFERENCES bom_revisions(id) ON DELETE CASCADE,
                    operation_sequence INT NOT NULL,
                    operation_name VARCHAR(100) NOT NULL,
                    work_center_id UUID NOT NULL,
                    setup_time_minutes NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
                    run_time_seconds_per_unit NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
                    labor_hours_per_unit NUMERIC(10, 4) NOT NULL DEFAULT 0.0000,
                    overlap_quantity NUMERIC(18, 4) NOT NULL DEFAULT 0.0000,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT uq_bom_op_seq UNIQUE (bom_revision_id, operation_sequence)
                );
            ");

            // 5. BOM ITEMS (COMPONENTS & SCRAP FACTORS)
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS bom_items (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    bom_revision_id UUID NOT NULL REFERENCES bom_revisions(id) ON DELETE CASCADE,
                    component_item_id UUID NOT NULL,
                    bom_operation_id UUID NULL REFERENCES bom_operations(id) ON DELETE SET NULL,
                    quantity_per_parent NUMERIC(18, 6) NOT NULL CHECK (quantity_per_parent > 0),
                    uom_id UUID NOT NULL,
                    scrap_factor_percentage NUMERIC(5, 2) NOT NULL DEFAULT 0.00 CHECK (scrap_factor_percentage >= 0),
                    is_critical_path BOOLEAN NOT NULL DEFAULT FALSE,
                    is_discontinued BOOLEAN NOT NULL DEFAULT FALSE,
                    substitute_item_id UUID NULL,
                    position_reference VARCHAR(50) NULL,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT uq_bom_item_component UNIQUE (bom_revision_id, component_item_id)
                );
            ");

            // 6. PRODUCTION PLANS (HEADER)
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS production_plans (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    plan_code VARCHAR(50) NOT NULL UNIQUE,
                    plan_name VARCHAR(150) NOT NULL,
                    start_date DATE NOT NULL,
                    end_date DATE NOT NULL,
                    status VARCHAR(30) NOT NULL DEFAULT 'DRAFT' CHECK (status IN ('DRAFT', 'CALCULATING_MRP', 'PLANNED', 'APPROVED', 'IN_EXECUTION', 'COMPLETED', 'CANCELLED')),
                    total_target_cost NUMERIC(18, 4) NOT NULL DEFAULT 0.0000,
                    created_by VARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
                    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    updated_by VARCHAR(100) NULL,
                    updated_at TIMESTAMPTZ NULL,
                    version_token INT NOT NULL DEFAULT 1
                );
            ");

            // 7. PRODUCTION PLAN ITEMS
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS production_plan_items (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    production_plan_id UUID NOT NULL REFERENCES production_plans(id) ON DELETE CASCADE,
                    sales_order_item_id UUID NULL,
                    finished_good_item_id UUID NOT NULL,
                    bom_revision_id UUID NOT NULL REFERENCES bom_revisions(id) ON DELETE RESTRICT,
                    planned_quantity NUMERIC(18, 4) NOT NULL CHECK (planned_quantity > 0),
                    completed_quantity NUMERIC(18, 4) NOT NULL DEFAULT 0.0000,
                    planned_start_date TIMESTAMPTZ NOT NULL,
                    planned_end_date TIMESTAMPTZ NOT NULL,
                    priority INT NOT NULL DEFAULT 1,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
                );
            ");

            // 8. PRODUCTION PLAN MATERIAL ALLOCATIONS
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS production_plan_material_allocations (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    production_plan_item_id UUID NOT NULL REFERENCES production_plan_items(id) ON DELETE CASCADE,
                    raw_material_item_id UUID NOT NULL,
                    warehouse_id UUID NOT NULL,
                    batch_id UUID NULL,
                    gross_required_qty NUMERIC(18, 6) NOT NULL,
                    soft_reserved_qty NUMERIC(18, 6) NOT NULL DEFAULT 0.000000,
                    hard_reserved_qty NUMERIC(18, 6) NOT NULL DEFAULT 0.000000,
                    shortage_qty NUMERIC(18, 6) NOT NULL DEFAULT 0.000000,
                    allocation_status VARCHAR(30) NOT NULL DEFAULT 'UNALLOCATED' CHECK (allocation_status IN ('UNALLOCATED', 'SOFT_RESERVED', 'HARD_RESERVED', 'SHORTAGE', 'ISSUED')),
                    indent_mrn_id UUID NULL,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMPTZ NULL
                );
            ");

            // 9. HIGH-PERFORMANCE NON-BLOCKING CONCURRENT INDEXES
            migrationBuilder.Sql(
                "CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_hub_metrics_timestamp ON production_hub_metrics_cache (snapshot_timestamp DESC);",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_boms_fg_item ON boms (finished_good_item_id) WHERE is_deleted = FALSE;",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_bom_revisions_lookup ON bom_revisions (bom_id, status, effective_start_date, effective_end_date);",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_bom_items_component ON bom_items (component_item_id);",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_plan_items_lookup ON production_plan_items (production_plan_id, finished_good_item_id);",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_allocations_item_status ON production_plan_material_allocations (raw_material_item_id, warehouse_id, allocation_status);",
                suppressTransaction: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS production_plan_material_allocations CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS production_plan_items CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS production_plans CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS bom_items CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS bom_operations CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS bom_revisions CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS boms CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS production_hub_metrics_cache CASCADE;");
        }
    }
}
