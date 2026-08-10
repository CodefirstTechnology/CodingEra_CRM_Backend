using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_ERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase9SalesHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 9 — Sales Module Production Hardening (infrastructure consolidation only).
            // No schema changes are required for this phase.
            // Changes delivered: shared DateHelper, PagedResult<T>, ApiResponse<T>,
            //   QuotationApprovalsController parity with DiscountApprovalsController,
            //   IQuotationApprovalRepository.ListSalesOrdersForLookupAsync,
            //   and mapper refactoring to eliminate date-helper duplication.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to undo — no schema changes were made in Phase 9.
        }
    }
}
