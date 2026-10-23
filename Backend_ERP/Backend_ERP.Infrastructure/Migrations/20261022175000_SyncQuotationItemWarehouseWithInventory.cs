using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    [DbContext(typeof(ERPDbContext))]
    [Migration("20261022175000_SyncQuotationItemWarehouseWithInventory")]
    public partial class SyncQuotationItemWarehouseWithInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DO $$
BEGIN
    -- Update Trigger Function: Validate & Synchronize Quotation Item Warehouse with Inventory
    CREATE OR REPLACE FUNCTION public.fn_validate_quotation_item_inventory_fk()
    RETURNS trigger AS $trg$
    DECLARE
        v_actual_wh integer;
    BEGIN
        IF NEW."ItemId" IS NOT NULL AND NEW."ItemType"::text = 'FinishedGood' THEN
            SELECT "WarehouseId" INTO v_actual_wh
            FROM public.finished_goods 
            WHERE "Id" = NEW."ItemId" 
              AND ("TenantId" = NEW."TenantId" OR "TenantId" = 'default');

            IF v_actual_wh IS NULL THEN
                RAISE EXCEPTION 'Inventory cross-table violation: FinishedGood ID % not found for tenant %',
                    NEW."ItemId", NEW."TenantId";
            END IF;

            -- If WarehouseId is omitted or defaulted to a warehouse where the item is not located, synchronize to the item's actual warehouse
            IF NEW."WarehouseId" IS NULL OR NEW."WarehouseId" != v_actual_wh THEN
                NEW."WarehouseId" := v_actual_wh;
            END IF;
        END IF;

        RETURN NEW;
    END;
    $trg$ LANGUAGE plpgsql;

    DROP TRIGGER IF EXISTS trg_validate_quotation_item_inventory_fk ON public.quotation_items;
    CREATE TRIGGER trg_validate_quotation_item_inventory_fk
        BEFORE INSERT OR UPDATE ON public.quotation_items
        FOR EACH ROW
        EXECUTE FUNCTION public.fn_validate_quotation_item_inventory_fk();

END $$;
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DO $$
BEGIN
    CREATE OR REPLACE FUNCTION public.fn_validate_quotation_item_inventory_fk()
    RETURNS trigger AS $trg$
    BEGIN
        IF NEW."ItemId" IS NOT NULL AND NEW."WarehouseId" IS NOT NULL THEN
            IF NEW."ItemType"::text = 'FinishedGood' THEN
                IF NOT EXISTS (
                    SELECT 1 FROM public.finished_goods 
                    WHERE "Id" = NEW."ItemId" 
                      AND "WarehouseId" = NEW."WarehouseId"
                      AND ("TenantId" = NEW."TenantId" OR "TenantId" = 'default')
                ) THEN
                    RAISE EXCEPTION 'Inventory cross-table violation: FinishedGood ID % in Warehouse % not found for tenant %',
                        NEW."ItemId", NEW."WarehouseId", NEW."TenantId";
                END IF;
            END IF;
        END IF;

        RETURN NEW;
    END;
    $trg$ LANGUAGE plpgsql;
END $$;
""");
        }
    }
}
