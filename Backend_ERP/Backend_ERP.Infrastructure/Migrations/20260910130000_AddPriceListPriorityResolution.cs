using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    [DbContext(typeof(ERPDbContext))]
    [Migration("20260910130000_AddPriceListPriorityResolution")]
    public partial class AddPriceListPriorityResolution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DO $$
BEGIN
    ALTER TABLE public.price_lists 
    ADD COLUMN IF NOT EXISTS "Priority" integer NOT NULL DEFAULT 0;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'price_list_history') THEN
        ALTER TABLE public.price_list_history ADD COLUMN IF NOT EXISTS "Priority" integer NULL;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'price_list_histories') THEN
        ALTER TABLE public.price_list_histories ADD COLUMN IF NOT EXISTS "Priority" integer NULL;
    END IF;

    CREATE INDEX IF NOT EXISTS "IX_price_lists_resolution" 
    ON public.price_lists ("Status", "CustomerCategory", "Currency", "Priority" DESC, "EffectiveFrom" DESC);
END $$;
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DO $$
BEGIN
    DROP INDEX IF EXISTS "IX_price_lists_resolution";

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'price_lists' AND column_name = 'Priority') THEN
        ALTER TABLE public.price_lists DROP COLUMN "Priority";
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'price_list_history' AND column_name = 'Priority') THEN
        ALTER TABLE public.price_list_history DROP COLUMN "Priority";
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'price_list_histories' AND column_name = 'Priority') THEN
        ALTER TABLE public.price_list_histories DROP COLUMN "Priority";
    END IF;
END $$;
""");
        }
    }
}
