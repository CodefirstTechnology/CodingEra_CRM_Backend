using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotationItemResolvedPriceListId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE public.quotation_items 
                ADD COLUMN IF NOT EXISTS ""ResolvedPriceListId"" integer NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE public.quotation_items 
                DROP COLUMN IF EXISTS ""ResolvedPriceListId"";
            ");
        }
    }
}
