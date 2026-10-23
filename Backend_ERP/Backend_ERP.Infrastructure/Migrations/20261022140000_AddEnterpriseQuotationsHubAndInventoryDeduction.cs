using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    [DbContext(typeof(ERPDbContext))]
    [Migration("20261022140000_AddEnterpriseQuotationsHubAndInventoryDeduction")]
    public partial class AddEnterpriseQuotationsHubAndInventoryDeduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DO $$
BEGIN
    -- 1. Enable Required PostgreSQL Extensions
    CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
    CREATE EXTENSION IF NOT EXISTS "btree_gist";

    -- 2. Alter Table: quotations (Revision Lineage, Concurrency, and Tenancy)
    ALTER TABLE public.quotations ADD COLUMN IF NOT EXISTS "TenantId" character varying(64) NOT NULL DEFAULT 'default';
    ALTER TABLE public.quotations ADD COLUMN IF NOT EXISTS "ParentQuotationId" integer NULL;
    ALTER TABLE public.quotations ADD COLUMN IF NOT EXISTS "RootQuotationId" integer NULL;
    ALTER TABLE public.quotations ADD COLUMN IF NOT EXISTS "RevisionNumber" integer NOT NULL DEFAULT 0;
    ALTER TABLE public.quotations ADD COLUMN IF NOT EXISTS "IsCurrentRevision" boolean NOT NULL DEFAULT TRUE;
    ALTER TABLE public.quotations ADD COLUMN IF NOT EXISTS "CustomerCategory" character varying(64) NOT NULL DEFAULT 'Standard';
    ALTER TABLE public.quotations ADD COLUMN IF NOT EXISTS "SalesPersonUserId" integer NULL;
    ALTER TABLE public.quotations ADD COLUMN IF NOT EXISTS "ApprovalToken" character varying(256) NULL;

    -- Add lineage foreign keys if not present
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'fk_quotations_parent' AND table_name = 'quotations'
    ) THEN
        ALTER TABLE public.quotations 
        ADD CONSTRAINT fk_quotations_parent 
        FOREIGN KEY ("ParentQuotationId") REFERENCES public.quotations ("Id") ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'fk_quotations_root' AND table_name = 'quotations'
    ) THEN
        ALTER TABLE public.quotations 
        ADD CONSTRAINT fk_quotations_root 
        FOREIGN KEY ("RootQuotationId") REFERENCES public.quotations ("Id") ON DELETE RESTRICT;
    END IF;

    -- Partial unique index guaranteeing only ONE active current revision per lineage tree
    CREATE UNIQUE INDEX IF NOT EXISTS uq_quotations_active_current_revision 
    ON public.quotations ("TenantId", "RootQuotationId") 
    WHERE "IsCurrentRevision" = TRUE AND "IsDeleted" = FALSE AND "RootQuotationId" IS NOT NULL;

    CREATE INDEX IF NOT EXISTS ix_quotations_tenant_number_rev 
    ON public.quotations ("TenantId", "QuotationNumber", "RevisionNumber");

    -- 3. Alter Table: quotation_items (Physical Inventory Interlinkage & Frozen Snapshots)
    ALTER TABLE public.quotation_items ADD COLUMN IF NOT EXISTS "TenantId" character varying(64) NOT NULL DEFAULT 'default';
    ALTER TABLE public.quotation_items ADD COLUMN IF NOT EXISTS "ItemType" character varying(32) NOT NULL DEFAULT 'FinishedGood';
    ALTER TABLE public.quotation_items ADD COLUMN IF NOT EXISTS "ItemId" integer NULL;
    ALTER TABLE public.quotation_items ADD COLUMN IF NOT EXISTS "WarehouseId" integer NULL;
    ALTER TABLE public.quotation_items ADD COLUMN IF NOT EXISTS "BaseStockUom" character varying(32) NOT NULL DEFAULT 'Nos';
    ALTER TABLE public.quotation_items ADD COLUMN IF NOT EXISTS "PriceListNumberSnapshot" character varying(64) NOT NULL DEFAULT '';
    ALTER TABLE public.quotation_items ADD COLUMN IF NOT EXISTS "ResolutionStageSnapshot" character varying(64) NOT NULL DEFAULT '';
    ALTER TABLE public.quotation_items ADD COLUMN IF NOT EXISTS "OriginalSellingRate" numeric(18,4) NOT NULL DEFAULT 0;
    ALTER TABLE public.quotation_items ADD COLUMN IF NOT EXISTS "FloorPriceSnapshot" numeric(18,4) NOT NULL DEFAULT 0;
    ALTER TABLE public.quotation_items ADD COLUMN IF NOT EXISTS "MaxDiscountPctSnapshot" numeric(9,4) NOT NULL DEFAULT 0;
    ALTER TABLE public.quotation_items ADD COLUMN IF NOT EXISTS "TaxSlabPercentSnapshot" numeric(9,4) NOT NULL DEFAULT 18;
    ALTER TABLE public.quotation_items ADD COLUMN IF NOT EXISTS "SnapshotTimestampUtc" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
    ALTER TABLE public.quotation_items ADD COLUMN IF NOT EXISTS "IsCommercialDeviation" boolean NOT NULL DEFAULT FALSE;

    CREATE INDEX IF NOT EXISTS ix_quotation_items_tenant_quote 
    ON public.quotation_items ("TenantId", "QuotationId");

    CREATE INDEX IF NOT EXISTS ix_quotation_items_tenant_itemcode 
    ON public.quotation_items ("TenantId", "ItemCode");

    CREATE INDEX IF NOT EXISTS ix_quotation_items_tenant_wh_item 
    ON public.quotation_items ("TenantId", "WarehouseId", "ItemId");

    -- 4. Alter Table: sales_orders (Tenancy & Cryptographic Approval Token)
    ALTER TABLE public.sales_orders ADD COLUMN IF NOT EXISTS "TenantId" character varying(64) NOT NULL DEFAULT 'default';
    ALTER TABLE public.sales_orders ADD COLUMN IF NOT EXISTS "ApprovalToken" character varying(256) NULL;

    CREATE INDEX IF NOT EXISTS ix_sales_orders_tenant_number 
    ON public.sales_orders ("TenantId", "SalesOrderNumber");

    -- 5. Alter Table: sales_order_items (Physical Item Linkage & Frozen Snapshots)
    ALTER TABLE public.sales_order_items ADD COLUMN IF NOT EXISTS "TenantId" character varying(64) NOT NULL DEFAULT 'default';
    ALTER TABLE public.sales_order_items ADD COLUMN IF NOT EXISTS "ItemType" character varying(32) NOT NULL DEFAULT 'FinishedGood';
    ALTER TABLE public.sales_order_items ADD COLUMN IF NOT EXISTS "ItemId" integer NULL;
    ALTER TABLE public.sales_order_items ADD COLUMN IF NOT EXISTS "WarehouseId" integer NULL;
    ALTER TABLE public.sales_order_items ADD COLUMN IF NOT EXISTS "ItemCode" character varying(64) NOT NULL DEFAULT '';
    ALTER TABLE public.sales_order_items ADD COLUMN IF NOT EXISTS "BaseStockUom" character varying(32) NOT NULL DEFAULT 'Nos';
    ALTER TABLE public.sales_order_items ADD COLUMN IF NOT EXISTS "DispatchedQuantity" numeric(18,4) NOT NULL DEFAULT 0;
    ALTER TABLE public.sales_order_items ADD COLUMN IF NOT EXISTS "ResolvedPriceListId" integer NULL;
    ALTER TABLE public.sales_order_items ADD COLUMN IF NOT EXISTS "PriceListNumberSnapshot" character varying(64) NOT NULL DEFAULT '';
    ALTER TABLE public.sales_order_items ADD COLUMN IF NOT EXISTS "OriginalSellingRate" numeric(18,4) NOT NULL DEFAULT 0;
    ALTER TABLE public.sales_order_items ADD COLUMN IF NOT EXISTS "FloorPriceSnapshot" numeric(18,4) NOT NULL DEFAULT 0;
    ALTER TABLE public.sales_order_items ADD COLUMN IF NOT EXISTS "MaxDiscountPctSnapshot" numeric(9,4) NOT NULL DEFAULT 0;
    ALTER TABLE public.sales_order_items ADD COLUMN IF NOT EXISTS "TaxSlabPercentSnapshot" numeric(9,4) NOT NULL DEFAULT 0;

    CREATE INDEX IF NOT EXISTS ix_sales_order_items_tenant_so 
    ON public.sales_order_items ("TenantId", "SalesOrderId");

    CREATE INDEX IF NOT EXISTS ix_sales_order_items_tenant_wh_item 
    ON public.sales_order_items ("TenantId", "WarehouseId", "ItemId");

    -- 6. Alter Table: finished_goods (Tenancy & Reorder Level)
    ALTER TABLE public.finished_goods ADD COLUMN IF NOT EXISTS "TenantId" character varying(64) NOT NULL DEFAULT 'default';
    ALTER TABLE public.finished_goods ADD COLUMN IF NOT EXISTS "ReorderLevel" numeric(18,4) NOT NULL DEFAULT 0;

    CREATE INDEX IF NOT EXISTS ix_finished_goods_tenant_code_wh 
    ON public.finished_goods ("TenantId", "ProductCode", "WarehouseId");

    -- 7. Alter Table: stock_transactions (Tenancy & Balance After)
    ALTER TABLE public.stock_transactions ADD COLUMN IF NOT EXISTS "TenantId" character varying(64) NOT NULL DEFAULT 'default';
    ALTER TABLE public.stock_transactions ADD COLUMN IF NOT EXISTS "BalanceAfter" numeric(18,4) NOT NULL DEFAULT 0;

    CREATE INDEX IF NOT EXISTS ix_stock_transactions_tenant_wh_mat 
    ON public.stock_transactions ("TenantId", "WarehouseId", "MaterialId");

    -- 8. Alter Table: QuotationApprovals
    ALTER TABLE public."QuotationApprovals" ADD COLUMN IF NOT EXISTS "TenantId" character varying(64) NOT NULL DEFAULT 'default';
    ALTER TABLE public."QuotationApprovals" ADD COLUMN IF NOT EXISTS "ApprovalToken" character varying(256) NULL;

    CREATE INDEX IF NOT EXISTS ix_quotation_approvals_tenant_status 
    ON public."QuotationApprovals" ("TenantId", "Status");

    -- 9. Alter Table: discount_approvals
    ALTER TABLE public.discount_approvals ADD COLUMN IF NOT EXISTS "TenantId" character varying(64) NOT NULL DEFAULT 'default';
    ALTER TABLE public.discount_approvals ADD COLUMN IF NOT EXISTS "ApprovalToken" character varying(256) NULL;
    ALTER TABLE public.discount_approvals ADD COLUMN IF NOT EXISTS "MaxAllowedDiscountPercentage" numeric(9,4) NOT NULL DEFAULT 0;
    ALTER TABLE public.discount_approvals ADD COLUMN IF NOT EXISTS "DiscountBreachPercentage" numeric(9,4) NOT NULL DEFAULT 0;

    CREATE INDEX IF NOT EXISTS ix_discount_approvals_tenant_status 
    ON public.discount_approvals ("TenantId", "Status");

    -- 10. Multi-tenant Triggers & Guard Functions
    -- Trigger A: Validate Quotation Item matches Parent Quotation Tenant
    CREATE OR REPLACE FUNCTION public.fn_validate_quotation_item_tenant()
    RETURNS trigger AS $trg$
    DECLARE
        v_parent_tenant character varying(64);
    BEGIN
        SELECT "TenantId" INTO v_parent_tenant 
        FROM public.quotations 
        WHERE "Id" = NEW."QuotationId";

        IF v_parent_tenant IS NOT NULL AND NEW."TenantId" <> v_parent_tenant THEN
            RAISE EXCEPTION 'Cross-tenant violation: QuotationItem tenant % does not match parent quotation tenant %', 
                NEW."TenantId", v_parent_tenant;
        END IF;

        RETURN NEW;
    END;
    $trg$ LANGUAGE plpgsql;

    DROP TRIGGER IF EXISTS trg_validate_quotation_item_tenant ON public.quotation_items;
    CREATE TRIGGER trg_validate_quotation_item_tenant
        BEFORE INSERT OR UPDATE ON public.quotation_items
        FOR EACH ROW
        EXECUTE FUNCTION public.fn_validate_quotation_item_tenant();

    -- Trigger B: Validate Quotation Item Inventory Foreign Key & Tenant
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
    DROP TRIGGER IF EXISTS trg_validate_quotation_item_inventory_fk ON public.quotation_items;
    DROP FUNCTION IF EXISTS public.fn_validate_quotation_item_inventory_fk();

    DROP TRIGGER IF EXISTS trg_validate_quotation_item_tenant ON public.quotation_items;
    DROP FUNCTION IF EXISTS public.fn_validate_quotation_item_tenant();

    DROP INDEX IF EXISTS public.uq_quotations_active_current_revision;
    DROP INDEX IF EXISTS public.ix_quotations_tenant_number_rev;
    DROP INDEX IF EXISTS public.ix_quotation_items_tenant_quote;
    DROP INDEX IF EXISTS public.ix_quotation_items_tenant_itemcode;
    DROP INDEX IF EXISTS public.ix_quotation_items_tenant_wh_item;
    DROP INDEX IF EXISTS public.ix_sales_orders_tenant_number;
    DROP INDEX IF EXISTS public.ix_sales_order_items_tenant_so;
    DROP INDEX IF EXISTS public.ix_sales_order_items_tenant_wh_item;
    DROP INDEX IF EXISTS public.ix_finished_goods_tenant_code_wh;
    DROP INDEX IF EXISTS public.ix_stock_transactions_tenant_wh_mat;
    DROP INDEX IF EXISTS public.ix_quotation_approvals_tenant_status;
    DROP INDEX IF EXISTS public.ix_discount_approvals_tenant_status;

    ALTER TABLE public.quotations DROP CONSTRAINT IF EXISTS fk_quotations_parent;
    ALTER TABLE public.quotations DROP CONSTRAINT IF EXISTS fk_quotations_root;
END $$;
""");
        }
    }
}
