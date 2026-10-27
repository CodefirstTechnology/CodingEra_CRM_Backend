using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesOrderNexusComplete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreditApprovedBy",
                table: "sales_orders",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreditApprovedDate",
                table: "sales_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreditHoldReason",
                table: "sales_orders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerCategory",
                table: "sales_orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Standard");

            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "sales_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DispatchReleasedBy",
                table: "sales_orders",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DispatchReleasedDate",
                table: "sales_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalInvoicedAmount",
                table: "sales_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "sales_orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDispatchReleased",
                table: "sales_orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsProductionReleased",
                table: "sales_orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PlaceOfSupplyStateCode",
                table: "sales_orders",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "27");

            migrationBuilder.AddColumn<string>(
                name: "ProductionReleasedBy",
                table: "sales_orders",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProductionReleasedDate",
                table: "sales_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AllocatedQuantity",
                table: "sales_order_items",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InvoicedQuantity",
                table: "sales_order_items",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PlaceOfSupplyStateCode",
                table: "sales_order_items",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "27");

            migrationBuilder.CreateTable(
                name: "sales_order_allocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "default"),
                    SalesOrderId = table.Column<int>(type: "integer", nullable: false),
                    SalesOrderItemId = table.Column<int>(type: "integer", nullable: false),
                    FinishedGoodId = table.Column<int>(type: "integer", nullable: false),
                    WarehouseId = table.Column<int>(type: "integer", nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: ""),
                    AllocatedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    AllocationStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AllocatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AllocatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ReleasedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReleaseRemarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_order_allocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sales_order_allocations_finished_goods_FinishedGoodId",
                        column: x => x.FinishedGoodId,
                        principalTable: "finished_goods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_order_allocations_sales_order_items_SalesOrderItemId",
                        column: x => x.SalesOrderItemId,
                        principalTable: "sales_order_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sales_order_allocations_sales_orders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "sales_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sales_order_allocations_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_TenantId_CustomerId_Status",
                table: "sales_orders",
                columns: new[] { "TenantId", "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_TenantId_Status_OrderDate",
                table: "sales_orders",
                columns: new[] { "TenantId", "Status", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_allocations_FinishedGoodId",
                table: "sales_order_allocations",
                column: "FinishedGoodId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_allocations_SalesOrderId",
                table: "sales_order_allocations",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_allocations_SalesOrderItemId",
                table: "sales_order_allocations",
                column: "SalesOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_allocations_WarehouseId",
                table: "sales_order_allocations",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "ix_so_allocations_lookup",
                table: "sales_order_allocations",
                columns: new[] { "TenantId", "FinishedGoodId", "WarehouseId", "AllocationStatus" });

            migrationBuilder.CreateIndex(
                name: "ix_so_allocations_order_status",
                table: "sales_order_allocations",
                columns: new[] { "TenantId", "SalesOrderId", "AllocationStatus" });

            // Idempotent Quotation Single-Conversion Guard Partial Index
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ux_quotations_converted_sales_order_id 
                ON quotations (""TenantId"", ""ConvertedSalesOrderId"") 
                WHERE ""ConvertedSalesOrderId"" IS NOT NULL;
            ");

            // Idempotent Trigger Function: Maintain sales_order_items.AllocatedQuantity consistency
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION fn_trg_sync_sales_order_item_allocation()
                RETURNS TRIGGER AS $$
                BEGIN
                    IF TG_OP = 'INSERT' OR TG_OP = 'UPDATE' THEN
                        UPDATE sales_order_items
                        SET ""AllocatedQuantity"" = COALESCE((
                            SELECT SUM(""AllocatedQuantity"") 
                            FROM sales_order_allocations 
                            WHERE ""SalesOrderItemId"" = NEW.""SalesOrderItemId"" 
                              AND ""AllocationStatus"" = 'Reserved'
                        ), 0)
                        WHERE ""Id"" = NEW.""SalesOrderItemId"";
                        RETURN NEW;
                    ELSIF TG_OP = 'DELETE' THEN
                        UPDATE sales_order_items
                        SET ""AllocatedQuantity"" = COALESCE((
                            SELECT SUM(""AllocatedQuantity"") 
                            FROM sales_order_allocations 
                            WHERE ""SalesOrderItemId"" = OLD.""SalesOrderItemId"" 
                              AND ""AllocationStatus"" = 'Reserved'
                        ), 0)
                        WHERE ""Id"" = OLD.""SalesOrderItemId"";
                        RETURN OLD;
                    END IF;
                END;
                $$ LANGUAGE plpgsql;

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_trigger 
                        WHERE tgname = 'trg_sync_so_item_allocation'
                    ) THEN
                        CREATE TRIGGER trg_sync_so_item_allocation
                        AFTER INSERT OR UPDATE OR DELETE ON sales_order_allocations
                        FOR EACH ROW EXECUTE FUNCTION fn_trg_sync_sales_order_item_allocation();
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS trg_sync_so_item_allocation ON sales_order_allocations;
                DROP FUNCTION IF EXISTS fn_trg_sync_sales_order_item_allocation();
                DROP INDEX IF EXISTS ux_quotations_converted_sales_order_id;
            ");

            migrationBuilder.DropTable(
                name: "sales_order_allocations");

            migrationBuilder.DropIndex(
                name: "IX_sales_orders_TenantId_CustomerId_Status",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "IX_sales_orders_TenantId_Status_OrderDate",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "CreditApprovedBy",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "CreditApprovedDate",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "CreditHoldReason",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "CustomerCategory",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "DispatchReleasedBy",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "DispatchReleasedDate",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "FinalInvoicedAmount",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "IsDispatchReleased",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "IsProductionReleased",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "PlaceOfSupplyStateCode",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "ProductionReleasedBy",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "ProductionReleasedDate",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "AllocatedQuantity",
                table: "sales_order_items");

            migrationBuilder.DropColumn(
                name: "InvoicedQuantity",
                table: "sales_order_items");

            migrationBuilder.DropColumn(
                name: "PlaceOfSupplyStateCode",
                table: "sales_order_items");
        }
    }
}
