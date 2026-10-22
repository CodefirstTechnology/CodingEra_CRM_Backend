using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    [DbContext(typeof(ERPDbContext))]
    [Migration("20261022133000_FixPriceListCanonicalDaterange")]
    public partial class FixPriceListCanonicalDaterange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DO $$
BEGIN
    -- Update all price lists to have canonical infinite upper bound when EffectiveTo is null
    -- Eliminates the year 10000 overflow when converting discrete daterange to .NET DateOnly
    UPDATE public.price_lists
    SET "ValidityPeriod" = CASE
        WHEN "EffectiveTo" IS NULL THEN daterange("EffectiveFrom", NULL, '[)')
        ELSE daterange("EffectiveFrom", "EffectiveTo", '[]')
    END;

    -- Update customer_price_list_mappings
    IF EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_schema = 'public' AND table_name = 'customer_price_list_mappings'
    ) THEN
        UPDATE public.customer_price_list_mappings
        SET "EffectiveRange" = CASE
            WHEN "EffectiveTo" IS NULL THEN daterange("EffectiveFrom", NULL, '[)')
            ELSE daterange("EffectiveFrom", "EffectiveTo", '[]')
        END;
    END IF;
END $$;
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
