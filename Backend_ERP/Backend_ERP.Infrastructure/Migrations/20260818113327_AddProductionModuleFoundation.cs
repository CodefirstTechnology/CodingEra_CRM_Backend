using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionModuleFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. production_machines
            migrationBuilder.CreateTable(
                name: "production_machines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MachineCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MachineName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Department = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RunningHours = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    IdleHours = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BreakdownHours = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UtilizationPercent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    MaintenanceDue = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_machines", x => x.Id);
                });

            // 2. production_boms
            migrationBuilder.CreateTable(
                name: "production_boms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BomNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Version = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Revision = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ProcessNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AttachmentName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_boms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_production_boms_finished_goods_ProductId",
                        column: x => x.ProductId,
                        principalTable: "finished_goods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // 3. production_bom_materials
            migrationBuilder.CreateTable(
                name: "production_bom_materials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BomId = table.Column<int>(type: "integer", nullable: false),
                    MaterialId = table.Column<int>(type: "integer", nullable: false),
                    MaterialCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MaterialName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Uom = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    WastagePercent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    WarehouseId = table.Column<int>(type: "integer", nullable: false),
                    WarehouseName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_bom_materials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_production_bom_materials_production_boms_BomId",
                        column: x => x.BomId,
                        principalTable: "production_boms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_production_bom_materials_raw_materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "raw_materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_bom_materials_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // 4. production_plans
            migrationBuilder.CreateTable(
                name: "production_plans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlanNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PlanningPeriod = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RequiredQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    PlannedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BomId = table.Column<int>(type: "integer", nullable: false),
                    BomNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WarehouseId = table.Column<int>(type: "integer", nullable: false),
                    WarehouseName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Priority = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Planner = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExpectedStart = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpectedFinish = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AttachmentName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_production_plans_finished_goods_ProductId",
                        column: x => x.ProductId,
                        principalTable: "finished_goods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_plans_production_boms_BomId",
                        column: x => x.BomId,
                        principalTable: "production_boms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_plans_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // 5. production_work_orders
            migrationBuilder.CreateTable(
                name: "production_work_orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkOrderNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PlanId = table.Column<int>(type: "integer", nullable: false),
                    PlanNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BomId = table.Column<int>(type: "integer", nullable: false),
                    BomNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PlannedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ProducedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    PendingQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Priority = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Supervisor = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MachineId = table.Column<int>(type: "integer", nullable: true),
                    MachineCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    MachineName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    AssignedTeam = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AttachmentName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_work_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_production_work_orders_finished_goods_ProductId",
                        column: x => x.ProductId,
                        principalTable: "finished_goods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_work_orders_production_boms_BomId",
                        column: x => x.BomId,
                        principalTable: "production_boms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_work_orders_production_machines_MachineId",
                        column: x => x.MachineId,
                        principalTable: "production_machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_production_work_orders_production_plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "production_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // 6. production_schedules
            migrationBuilder.CreateTable(
                name: "production_schedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ScheduleNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WorkOrderId = table.Column<int>(type: "integer", nullable: false),
                    WorkOrderNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MachineId = table.Column<int>(type: "integer", nullable: false),
                    MachineCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MachineName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Shift = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Operator = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Capacity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UtilizationPercent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DelayMinutes = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_production_schedules_production_machines_MachineId",
                        column: x => x.MachineId,
                        principalTable: "production_machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_schedules_production_work_orders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "production_work_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // 7. production_entries
            migrationBuilder.CreateTable(
                name: "production_entries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntryNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WorkOrderId = table.Column<int>(type: "integer", nullable: false),
                    WorkOrderNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProducedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    GoodQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    RejectedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Shift = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Operator = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MachineId = table.Column<int>(type: "integer", nullable: false),
                    MachineCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MachineName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProductionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AttachmentName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_production_entries_finished_goods_ProductId",
                        column: x => x.ProductId,
                        principalTable: "finished_goods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_entries_production_machines_MachineId",
                        column: x => x.MachineId,
                        principalTable: "production_machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_entries_production_work_orders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "production_work_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // 8. production_consumptions
            migrationBuilder.CreateTable(
                name: "production_consumptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConsumptionNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WorkOrderId = table.Column<int>(type: "integer", nullable: false),
                    WorkOrderNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BomId = table.Column<int>(type: "integer", nullable: false),
                    BomNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntryId = table.Column<int>(type: "integer", nullable: true),
                    MaterialId = table.Column<int>(type: "integer", nullable: false),
                    MaterialCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MaterialName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PlannedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ActualQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Variance = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Uom = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    WarehouseId = table.Column<int>(type: "integer", nullable: false),
                    WarehouseName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    StockOutReference = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_consumptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_production_consumptions_production_boms_BomId",
                        column: x => x.BomId,
                        principalTable: "production_boms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_consumptions_production_entries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "production_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_production_consumptions_production_work_orders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "production_work_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_consumptions_raw_materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "raw_materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_consumptions_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // 9. production_rejections
            migrationBuilder.CreateTable(
                name: "production_rejections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RejectionNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WorkOrderId = table.Column<int>(type: "integer", nullable: false),
                    WorkOrderNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntryId = table.Column<int>(type: "integer", nullable: true),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Operator = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MachineId = table.Column<int>(type: "integer", nullable: false),
                    MachineCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MachineName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RejectionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CorrectiveAction = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_rejections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_production_rejections_finished_goods_ProductId",
                        column: x => x.ProductId,
                        principalTable: "finished_goods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_rejections_production_entries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "production_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_production_rejections_production_machines_MachineId",
                        column: x => x.MachineId,
                        principalTable: "production_machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_rejections_production_work_orders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "production_work_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // 10. production_document_sequences
            migrationBuilder.CreateTable(
                name: "production_document_sequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Prefix = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LastSequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_document_sequences", x => x.Id);
                });

            // Indices
            migrationBuilder.CreateIndex(name: "IX_production_boms_BomNumber", table: "production_boms", column: "BomNumber", unique: true);
            migrationBuilder.CreateIndex(name: "IX_production_boms_IsDeleted", table: "production_boms", column: "IsDeleted");
            migrationBuilder.CreateIndex(name: "IX_production_boms_ProductId", table: "production_boms", column: "ProductId");
            migrationBuilder.CreateIndex(name: "IX_production_boms_Status", table: "production_boms", column: "Status");

            migrationBuilder.CreateIndex(name: "IX_production_bom_materials_BomId", table: "production_bom_materials", column: "BomId");
            migrationBuilder.CreateIndex(name: "IX_production_bom_materials_MaterialId", table: "production_bom_materials", column: "MaterialId");
            migrationBuilder.CreateIndex(name: "IX_production_bom_materials_WarehouseId", table: "production_bom_materials", column: "WarehouseId");

            migrationBuilder.CreateIndex(name: "IX_production_plans_BomId", table: "production_plans", column: "BomId");
            migrationBuilder.CreateIndex(name: "IX_production_plans_IsDeleted", table: "production_plans", column: "IsDeleted");
            migrationBuilder.CreateIndex(name: "IX_production_plans_PlanNumber", table: "production_plans", column: "PlanNumber", unique: true);
            migrationBuilder.CreateIndex(name: "IX_production_plans_ProductId", table: "production_plans", column: "ProductId");
            migrationBuilder.CreateIndex(name: "IX_production_plans_Status", table: "production_plans", column: "Status");
            migrationBuilder.CreateIndex(name: "IX_production_plans_WarehouseId", table: "production_plans", column: "WarehouseId");

            migrationBuilder.CreateIndex(name: "IX_production_work_orders_BomId", table: "production_work_orders", column: "BomId");
            migrationBuilder.CreateIndex(name: "IX_production_work_orders_IsDeleted", table: "production_work_orders", column: "IsDeleted");
            migrationBuilder.CreateIndex(name: "IX_production_work_orders_MachineId", table: "production_work_orders", column: "MachineId");
            migrationBuilder.CreateIndex(name: "IX_production_work_orders_PlanId", table: "production_work_orders", column: "PlanId");
            migrationBuilder.CreateIndex(name: "IX_production_work_orders_ProductId", table: "production_work_orders", column: "ProductId");
            migrationBuilder.CreateIndex(name: "IX_production_work_orders_Status", table: "production_work_orders", column: "Status");
            migrationBuilder.CreateIndex(name: "IX_production_work_orders_WorkOrderNumber", table: "production_work_orders", column: "WorkOrderNumber", unique: true);

            migrationBuilder.CreateIndex(name: "IX_production_schedules_IsDeleted", table: "production_schedules", column: "IsDeleted");
            migrationBuilder.CreateIndex(name: "IX_production_schedules_MachineId", table: "production_schedules", column: "MachineId");
            migrationBuilder.CreateIndex(name: "IX_production_schedules_ScheduleNumber", table: "production_schedules", column: "ScheduleNumber", unique: true);
            migrationBuilder.CreateIndex(name: "IX_production_schedules_Status", table: "production_schedules", column: "Status");
            migrationBuilder.CreateIndex(name: "IX_production_schedules_WorkOrderId", table: "production_schedules", column: "WorkOrderId");

            migrationBuilder.CreateIndex(name: "IX_production_machines_IsDeleted", table: "production_machines", column: "IsDeleted");
            migrationBuilder.CreateIndex(name: "IX_production_machines_MachineCode", table: "production_machines", column: "MachineCode", unique: true);
            migrationBuilder.CreateIndex(name: "IX_production_machines_Status", table: "production_machines", column: "Status");

            migrationBuilder.CreateIndex(name: "IX_production_entries_EntryNumber", table: "production_entries", column: "EntryNumber", unique: true);
            migrationBuilder.CreateIndex(name: "IX_production_entries_IsDeleted", table: "production_entries", column: "IsDeleted");
            migrationBuilder.CreateIndex(name: "IX_production_entries_MachineId", table: "production_entries", column: "MachineId");
            migrationBuilder.CreateIndex(name: "IX_production_entries_ProductId", table: "production_entries", column: "ProductId");
            migrationBuilder.CreateIndex(name: "IX_production_entries_Status", table: "production_entries", column: "Status");
            migrationBuilder.CreateIndex(name: "IX_production_entries_WorkOrderId", table: "production_entries", column: "WorkOrderId");

            migrationBuilder.CreateIndex(name: "IX_production_consumptions_BomId", table: "production_consumptions", column: "BomId");
            migrationBuilder.CreateIndex(name: "IX_production_consumptions_ConsumptionNumber", table: "production_consumptions", column: "ConsumptionNumber", unique: true);
            migrationBuilder.CreateIndex(name: "IX_production_consumptions_EntryId", table: "production_consumptions", column: "EntryId");
            migrationBuilder.CreateIndex(name: "IX_production_consumptions_IsDeleted", table: "production_consumptions", column: "IsDeleted");
            migrationBuilder.CreateIndex(name: "IX_production_consumptions_MaterialId", table: "production_consumptions", column: "MaterialId");
            migrationBuilder.CreateIndex(name: "IX_production_consumptions_WarehouseId", table: "production_consumptions", column: "WarehouseId");
            migrationBuilder.CreateIndex(name: "IX_production_consumptions_WorkOrderId", table: "production_consumptions", column: "WorkOrderId");

            migrationBuilder.CreateIndex(name: "IX_production_rejections_EntryId", table: "production_rejections", column: "EntryId");
            migrationBuilder.CreateIndex(name: "IX_production_rejections_IsDeleted", table: "production_rejections", column: "IsDeleted");
            migrationBuilder.CreateIndex(name: "IX_production_rejections_MachineId", table: "production_rejections", column: "MachineId");
            migrationBuilder.CreateIndex(name: "IX_production_rejections_ProductId", table: "production_rejections", column: "ProductId");
            migrationBuilder.CreateIndex(name: "IX_production_rejections_RejectionNumber", table: "production_rejections", column: "RejectionNumber", unique: true);
            migrationBuilder.CreateIndex(name: "IX_production_rejections_WorkOrderId", table: "production_rejections", column: "WorkOrderId");

            migrationBuilder.CreateIndex(name: "IX_production_document_sequences_Prefix", table: "production_document_sequences", column: "Prefix", unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "production_bom_materials");
            migrationBuilder.DropTable(name: "production_consumptions");
            migrationBuilder.DropTable(name: "production_rejections");
            migrationBuilder.DropTable(name: "production_entries");
            migrationBuilder.DropTable(name: "production_schedules");
            migrationBuilder.DropTable(name: "production_work_orders");
            migrationBuilder.DropTable(name: "production_plans");
            migrationBuilder.DropTable(name: "production_boms");
            migrationBuilder.DropTable(name: "production_machines");
            migrationBuilder.DropTable(name: "production_document_sequences");
        }
    }
}
