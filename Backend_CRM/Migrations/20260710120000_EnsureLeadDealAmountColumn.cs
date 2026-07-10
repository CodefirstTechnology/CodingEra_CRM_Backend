using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    public partial class EnsureLeadDealAmountColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE leads ADD COLUMN IF NOT EXISTS deal_amount numeric NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
