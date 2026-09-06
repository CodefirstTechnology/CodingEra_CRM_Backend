using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    [DbContext(typeof(ERPDbContext))]
    [Migration("20260906113000_AddQuotationDiscountApprovalLinkage")]
    public partial class AddQuotationDiscountApprovalLinkage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DO $$
BEGIN
    ALTER TABLE quotations ADD COLUMN IF NOT EXISTS "DiscountApprovalId" integer NULL REFERENCES discount_approvals("Id") ON DELETE SET NULL;
    ALTER TABLE quotations ADD COLUMN IF NOT EXISTS "DiscountApprovalStatus" character varying(32) NOT NULL DEFAULT 'None';
    CREATE INDEX IF NOT EXISTS "IX_quotations_DiscountApprovalId" ON quotations ("DiscountApprovalId");
END $$;
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DROP INDEX IF EXISTS "IX_quotations_DiscountApprovalId";
ALTER TABLE quotations DROP COLUMN IF EXISTS "DiscountApprovalStatus";
ALTER TABLE quotations DROP COLUMN IF EXISTS "DiscountApprovalId";
""");
        }
    }
}
