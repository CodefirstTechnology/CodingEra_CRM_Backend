using System;
using System.Collections.Generic;
using System.Security.Claims;
using ERP.Application.Common.Security;
using ERP.Domain.Enums;
using ERP.Domain.Sales;
using ERP.Infrastructure.Security;
using ERP.Shared.Security;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Backend_ERP.Tests
{
    public class ErpRbacPhase3Tests
    {
        private static (ICurrentUser currentUser, IErpAuthorizationService authService, IErpWorkflowAuthorizationService workflowAuth) CreateContext(
            int userId,
            string role,
            string email = "sales@company.com",
            string fullName = "Jane Doe",
            IEnumerable<string>? extraPermissions = null)
        {
            var claims = new List<Claim>
            {
                new("userId", userId.ToString()),
                new("role", role),
                new("email", email),
                new("fullName", fullName),
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Role, role),
                new(ClaimTypes.Email, email),
                new(ClaimTypes.Name, fullName)
            };

            if (extraPermissions != null)
            {
                foreach (var p in extraPermissions)
                {
                    claims.Add(new Claim("permission", p));
                }
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };
            var accessor = new TestHttpContextAccessor { HttpContext = httpContext };

            var currentUser = new CurrentUser(accessor);
            var authService = new ErpAuthorizationService(currentUser);
            var workflowAuth = new ErpWorkflowAuthorizationService(currentUser, authService);

            return (currentUser, authService, workflowAuth);
        }

        // =========================================================================
        // SECTION A: Quotation Workflow (Tests 1–15)
        // =========================================================================

        [Fact]
        public void Test01_SalesExecutive_CanEditDraftOwnQuotation()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.True(workflow.CanEditQuotation(QuotationApprovalStatuses.Draft, 101));
        }

        [Fact]
        public void Test02_SalesExecutive_CanEditReturnedOwnQuotation()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.True(workflow.CanEditQuotation(QuotationApprovalStatuses.Returned, 101));
            Assert.True(workflow.CanEditQuotation(QuotationApprovalStatuses.RevisionRequired, 101));
        }

        [Fact]
        public void Test03_SalesExecutive_CannotEditApprovedQuotation()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanEditQuotation(QuotationApprovalStatuses.Approved, 101));
        }

        [Fact]
        public void Test04_SalesExecutive_CannotEditAnotherUsersDraftQuotation()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanEditQuotation(QuotationApprovalStatuses.Draft, 102));
        }

        [Fact]
        public void Test05_SalesExecutive_CanSubmitOwnDraftQuotation()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.True(workflow.CanSubmitQuotation(QuotationApprovalStatuses.Draft, 101));
        }

        [Fact]
        public void Test06_SalesExecutive_CannotApproveQuotation()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanApproveQuotation(QuotationApprovalStatuses.Submitted, 101));
            Assert.False(workflow.CanApproveQuotation(QuotationApprovalStatuses.UnderReview, 102));
        }

        [Fact]
        public void Test07_SalesExecutive_CannotRejectQuotation()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanRejectQuotation(QuotationApprovalStatuses.UnderReview, 101));
        }

        [Fact]
        public void Test08_SalesExecutive_CannotReturnQuotation()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanReturnQuotation(QuotationApprovalStatuses.UnderReview, 101));
        }

        [Fact]
        public void Test09_SalesExecutive_CannotReopenQuotation()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanReopenQuotation(QuotationApprovalStatuses.Cancelled, 101));
            Assert.False(workflow.CanReopenQuotation(QuotationApprovalStatuses.Rejected, 101));
        }

        [Fact]
        public void Test10_SalesExecutive_CannotDeleteApprovedOrSubmittedQuotation()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanDeleteQuotation(QuotationApprovalStatuses.Approved, 101));
            Assert.False(workflow.CanDeleteQuotation(QuotationApprovalStatuses.Submitted, 101));
        }

        [Fact]
        public void Test11_Quotation_InvalidStatusTransitions_Blocked()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanEditQuotation(QuotationApprovalStatuses.Cancelled, 101));
            Assert.False(workflow.CanEditQuotation(QuotationApprovalStatuses.Rejected, 101));
            Assert.False(workflow.CanSubmitQuotation(QuotationApprovalStatuses.Approved, 101));
        }

        [Fact]
        public void Test12_Admin_CanApproveQuotation()
        {
            var (_, _, workflow) = CreateContext(1, "Admin");
            Assert.True(workflow.CanApproveQuotation(QuotationApprovalStatuses.Submitted, 101));
            Assert.True(workflow.CanApproveQuotation(QuotationApprovalStatuses.UnderReview, 101));
        }

        [Fact]
        public void Test13_Admin_CanRejectQuotation()
        {
            var (_, _, workflow) = CreateContext(1, "Admin");
            Assert.True(workflow.CanRejectQuotation(QuotationApprovalStatuses.UnderReview, 101));
        }

        [Fact]
        public void Test14_Admin_CanReturnQuotation()
        {
            var (_, _, workflow) = CreateContext(1, "Admin");
            Assert.True(workflow.CanReturnQuotation(QuotationApprovalStatuses.UnderReview, 101));
        }

        [Fact]
        public void Test15_Admin_CanReopenQuotation()
        {
            var (_, _, workflow) = CreateContext(1, "Admin");
            Assert.True(workflow.CanReopenQuotation(QuotationApprovalStatuses.Cancelled, 101));
            Assert.True(workflow.CanReopenQuotation(QuotationApprovalStatuses.Rejected, 101));
        }

        // =========================================================================
        // SECTION B: Discount Approval Workflow (Tests 16–24)
        // =========================================================================

        [Fact]
        public void Test16_SalesExecutive_CanCreateOwnDiscountRequest()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.True(workflow.CanCreateDiscountApproval(101));
        }

        [Fact]
        public void Test17_SalesExecutive_CanResubmitReturnedDiscountRequest()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.True(workflow.CanResubmitDiscountApproval(DiscountApprovalStatuses.Returned, 101));
        }

        [Fact]
        public void Test18_SalesExecutive_CannotApproveOwnDiscountRequest()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanApproveDiscountApproval(DiscountApprovalStatuses.Pending, 101));
        }

        [Fact]
        public void Test19_SalesExecutive_CannotApproveAnotherUsersDiscountRequest()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanApproveDiscountApproval(DiscountApprovalStatuses.Pending, 102));
        }

        [Fact]
        public void Test20_SalesExecutive_CannotCancelDiscountRequest()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanCancelDiscountApproval(DiscountApprovalStatuses.Pending, 101));
        }

        [Fact]
        public void Test21_Admin_CanApproveDiscountRequest()
        {
            var (_, _, workflow) = CreateContext(1, "Admin");
            Assert.True(workflow.CanApproveDiscountApproval(DiscountApprovalStatuses.Pending, 101));
            Assert.True(workflow.CanApproveDiscountApproval(DiscountApprovalStatuses.UnderReview, 101));
        }

        [Fact]
        public void Test22_Admin_CanRejectDiscountRequest()
        {
            var (_, _, workflow) = CreateContext(1, "Admin");
            Assert.True(workflow.CanRejectDiscountApproval(DiscountApprovalStatuses.Pending, 101));
        }

        [Fact]
        public void Test23_Admin_CanReturnDiscountRequest()
        {
            var (_, _, workflow) = CreateContext(1, "Admin");
            Assert.True(workflow.CanReturnDiscountApproval(DiscountApprovalStatuses.Pending, 101));
        }

        [Fact]
        public void Test24_SalesExecutive_CannotCreateDiscountForAnotherUsersQuotation()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanCreateDiscountApproval(102));
        }

        // =========================================================================
        // SECTION C: Sales Orders Workflow (Tests 25–36)
        // =========================================================================

        [Fact]
        public void Test25_SalesExecutive_CanCreateOwnSalesOrder()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            Assert.True(auth.Authorize(ErpPermissions.SalesOrders.Create));
        }

        [Fact]
        public void Test26_SalesExecutive_CanSubmitOwnSalesOrder()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.True(workflow.CanSubmitSalesOrder(SalesOrderStatuses.Draft, "101"));
        }

        [Fact]
        public void Test27_SalesExecutive_CannotConfirmSalesOrder()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanConfirmSalesOrder(SalesOrderStatuses.Submitted, "101"));
        }

        [Fact]
        public void Test28_SalesExecutive_CannotProcessSalesOrder()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanProcessSalesOrder(SalesOrderStatuses.Confirmed, "101"));
        }

        [Fact]
        public void Test29_SalesExecutive_CannotCompleteSalesOrder()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanCompleteSalesOrder(SalesOrderStatuses.Processing, "101"));
        }

        [Fact]
        public void Test30_SalesExecutive_CannotCancelSalesOrder()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanCancelSalesOrder(SalesOrderStatuses.Submitted, "101"));
        }

        [Fact]
        public void Test31_SalesExecutive_CannotEditConfirmedSalesOrder()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanEditSalesOrder(SalesOrderStatuses.Confirmed, "101"));
            Assert.False(workflow.CanEditSalesOrder(SalesOrderStatuses.Completed, "101"));
        }

        [Fact]
        public void Test32_SalesExecutive_CannotEditAnotherUsersSalesOrder()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanEditSalesOrder(SalesOrderStatuses.Draft, "102"));
        }

        [Fact]
        public void Test33_Admin_CanConfirmSalesOrder()
        {
            var (_, _, workflow) = CreateContext(1, "Admin");
            Assert.True(workflow.CanConfirmSalesOrder(SalesOrderStatuses.Submitted, "101"));
        }

        [Fact]
        public void Test34_Admin_CanProcessSalesOrder()
        {
            var (_, _, workflow) = CreateContext(1, "Admin");
            Assert.True(workflow.CanProcessSalesOrder(SalesOrderStatuses.Confirmed, "101"));
        }

        [Fact]
        public void Test35_Admin_CanCompleteSalesOrder()
        {
            var (_, _, workflow) = CreateContext(1, "Admin");
            Assert.True(workflow.CanCompleteSalesOrder(SalesOrderStatuses.Processing, "101"));
            Assert.True(workflow.CanCompleteSalesOrder(SalesOrderStatuses.PartiallyDelivered, "101"));
        }

        [Fact]
        public void Test36_Admin_CanCancelSalesOrder()
        {
            var (_, _, workflow) = CreateContext(1, "Admin");
            Assert.True(workflow.CanCancelSalesOrder(SalesOrderStatuses.Submitted, "101"));
            Assert.False(workflow.CanCancelSalesOrder(SalesOrderStatuses.Completed, "101"));
        }

        // =========================================================================
        // SECTION D: Proforma Invoice Workflow (Tests 37–45)
        // =========================================================================

        [Fact]
        public void Test37_SalesExecutive_CanCreateProformaInvoice()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            Assert.True(auth.Authorize(ErpPermissions.ProformaInvoices.Create));
        }

        [Fact]
        public void Test38_SalesExecutive_CanSubmitProformaInvoice()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.True(workflow.CanSubmitProformaInvoice(ProformaInvoiceStatuses.Draft, "101"));
        }

        [Fact]
        public void Test39_SalesExecutive_CannotApproveProformaInvoice()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanApproveProformaInvoice(ProformaInvoiceStatuses.Submitted, "101"));
        }

        [Fact]
        public void Test40_SalesExecutive_CannotRejectProformaInvoice()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanRejectProformaInvoice(ProformaInvoiceStatuses.Submitted, "101"));
        }

        [Fact]
        public void Test41_SalesExecutive_CannotConvertProformaInvoice()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanConvertProformaInvoice(ProformaInvoiceStatuses.Approved, false));
        }

        [Fact]
        public void Test42_SalesExecutive_CannotDirectlySetProformaApproved()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanEditProformaInvoice(ProformaInvoiceStatuses.Approved, "101"));
        }

        [Fact]
        public void Test43_CannotConvertUnapprovedProformaInvoice()
        {
            var (_, _, workflow) = CreateContext(1, "Admin");
            Assert.False(workflow.CanConvertProformaInvoice(ProformaInvoiceStatuses.Draft, false));
            Assert.False(workflow.CanConvertProformaInvoice(ProformaInvoiceStatuses.Submitted, false));
            Assert.False(workflow.CanConvertProformaInvoice(ProformaInvoiceStatuses.Rejected, false));
        }

        [Fact]
        public void Test44_Admin_CanApproveProformaInvoice()
        {
            var (_, _, workflow) = CreateContext(1, "Admin");
            Assert.True(workflow.CanApproveProformaInvoice(ProformaInvoiceStatuses.Submitted, "101"));
            Assert.True(workflow.CanApproveProformaInvoice(ProformaInvoiceStatuses.PendingFinanceApproval, "101"));
        }

        [Fact]
        public void Test45_Admin_CanConvertApprovedProformaInvoice_Once()
        {
            var (_, _, workflow) = CreateContext(1, "Admin");
            Assert.True(workflow.CanConvertProformaInvoice(ProformaInvoiceStatuses.Approved, false));
            Assert.False(workflow.CanConvertProformaInvoice(ProformaInvoiceStatuses.Approved, true));
        }

        // =========================================================================
        // SECTION E: Advance Payment Workflow (Tests 46–54)
        // =========================================================================

        [Fact]
        public void Test46_SalesExecutive_CanCreateAdvancePayment()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            Assert.True(auth.Authorize(ErpPermissions.AdvancePayments.Create));
        }

        [Fact]
        public void Test47_SalesExecutive_CanViewOwnAdvancePayment()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            Assert.True(auth.CanAccessRecord("101"));
        }

        [Fact]
        public void Test48_SalesExecutive_CannotVerifyAdvancePayment()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanVerifyAdvancePayment(AdvancePaymentStatuses.Submitted));
        }

        [Fact]
        public void Test49_SalesExecutive_CannotReceiveAdvancePayment()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanReceiveAdvancePayment(AdvancePaymentStatuses.FinanceVerification));
        }

        [Fact]
        public void Test50_SalesExecutive_CannotEditVerifiedAdvancePayment()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanEditAdvancePayment(AdvancePaymentStatuses.Received, "101"));
        }

        [Fact]
        public void Test51_SalesExecutive_CannotApplyUnreceivedAdvancePayment()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanApplyAdvancePayment(AdvancePaymentStatuses.Draft, 5000m, "101"));
            Assert.False(workflow.CanApplyAdvancePayment(AdvancePaymentStatuses.Submitted, 5000m, "101"));
            Assert.False(workflow.CanApplyAdvancePayment(AdvancePaymentStatuses.FinanceVerification, 5000m, "101"));
        }

        [Fact]
        public void Test52_Admin_CanVerifyAdvancePayment()
        {
            var (_, _, workflow) = CreateContext(1, "Admin");
            Assert.True(workflow.CanVerifyAdvancePayment(AdvancePaymentStatuses.Submitted));
        }

        [Fact]
        public void Test53_Admin_CanReceiveAdvancePayment()
        {
            var (_, _, workflow) = CreateContext(1, "Admin");
            Assert.True(workflow.CanReceiveAdvancePayment(AdvancePaymentStatuses.Submitted));
            Assert.True(workflow.CanReceiveAdvancePayment(AdvancePaymentStatuses.FinanceVerification));
        }

        [Fact]
        public void Test54_ValidReceivedPayment_CanBeAppliedToOwnOrder()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.True(workflow.CanApplyAdvancePayment(AdvancePaymentStatuses.Received, 5000m, "101"));
            Assert.False(workflow.CanApplyAdvancePayment(AdvancePaymentStatuses.Received, 0m, "101"));
            Assert.False(workflow.CanApplyAdvancePayment(AdvancePaymentStatuses.Received, 5000m, "102"));
        }

        // =========================================================================
        // SECTION F: Sales Targets (Tests 55–59)
        // =========================================================================

        [Fact]
        public void Test55_SalesExecutive_CanViewOwnTarget()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            Assert.True(auth.Authorize(ErpPermissions.SalesTargets.View));
            Assert.True(auth.CanAccessRecord(101));
        }

        [Fact]
        public void Test56_SalesExecutive_CannotCreateOrEditTargets()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            Assert.False(auth.Authorize(ErpPermissions.SalesTargets.Create));
            Assert.False(auth.Authorize(ErpPermissions.SalesTargets.Edit));
        }

        [Fact]
        public void Test57_SalesExecutive_CannotDeleteOrDuplicateTargets()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            Assert.False(auth.Authorize(ErpPermissions.SalesTargets.Delete));
            Assert.False(auth.Authorize(ErpPermissions.SalesTargets.Duplicate));
        }

        [Fact]
        public void Test58_SalesExecutive_CannotAccessOtherUsersTarget()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            Assert.False(auth.CanAccessRecord(102));
        }

        [Fact]
        public void Test59_Admin_CanManageAllTargets()
        {
            var (_, auth, _) = CreateContext(1, "Admin");
            Assert.True(auth.Authorize(ErpPermissions.SalesTargets.Create));
            Assert.True(auth.Authorize(ErpPermissions.SalesTargets.Edit));
            Assert.True(auth.Authorize(ErpPermissions.SalesTargets.Delete));
            Assert.True(auth.CanAccessRecord(101));
            Assert.True(auth.CanAccessRecord(102));
        }

        // =========================================================================
        // SECTION G: Performance Security (Tests 60–64)
        // =========================================================================

        [Fact]
        public void Test60_SalesExecutive_CanViewPersonalPerformance()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            Assert.True(auth.Authorize(ErpPermissions.Performance.View));
        }

        [Fact]
        public void Test61_SalesExecutive_CannotViewOtherUsersPerformance()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            Assert.False(auth.CanAccessRecord(102));
        }

        [Fact]
        public void Test62_SalesExecutive_CannotViewLeaderboards()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            Assert.False(auth.Authorize(ErpPermissions.Performance.LeaderboardView));
        }

        [Fact]
        public void Test63_SalesExecutive_CannotExportPerformance()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            Assert.False(auth.Authorize(ErpPermissions.Performance.Export));
        }

        [Fact]
        public void Test64_Admin_CanAccessAllPerformanceMetrics()
        {
            var (_, auth, _) = CreateContext(1, "Admin");
            Assert.True(auth.Authorize(ErpPermissions.Performance.View));
            Assert.True(auth.Authorize(ErpPermissions.Performance.LeaderboardView));
            Assert.True(auth.Authorize(ErpPermissions.Performance.Export));
        }

        // =========================================================================
        // SECTION H: Cross-Module Authorization (Tests 65–70)
        // =========================================================================

        [Fact]
        public void Test65_SalesExecutive_CannotCreateSalesOrderFromAnotherUsersQuotation()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanCreateSalesOrderFromQuotation(QuotationApprovalStatuses.Approved, 102));
        }

        [Fact]
        public void Test66_SalesExecutive_CannotCreateSalesOrderFromUnapprovedQuotation()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanCreateSalesOrderFromQuotation(QuotationApprovalStatuses.Draft, 101));
            Assert.False(workflow.CanCreateSalesOrderFromQuotation(QuotationApprovalStatuses.Submitted, 101));
        }

        [Fact]
        public void Test67_SalesExecutive_CannotCreateProformaFromAnotherUsersSalesOrder()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanCreateProformaFromSalesOrder(SalesOrderStatuses.Draft, "102"));
        }

        [Fact]
        public void Test68_SalesExecutive_CannotApplyAdvancePaymentToAnotherUsersSalesOrder()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanApplyAdvancePayment(AdvancePaymentStatuses.Received, 5000m, "102"));
        }

        [Fact]
        public void Test69_SalesExecutive_CannotCreateDiscountForAnotherUsersQuotation()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanCreateDiscountApproval(102));
        }

        [Fact]
        public void Test70_Admin_CanPerformValidCrossModuleOperations()
        {
            var (_, _, workflow) = CreateContext(1, "Admin");
            Assert.True(workflow.CanCreateSalesOrderFromQuotation(QuotationApprovalStatuses.Approved, 101));
            Assert.True(workflow.CanCreateProformaFromSalesOrder(SalesOrderStatuses.Draft, "101"));
            Assert.True(workflow.CanApplyAdvancePayment(AdvancePaymentStatuses.Received, 5000m, "101"));
        }

        // =========================================================================
        // SECTION I: IDOR Security (Tests 71–77)
        // =========================================================================

        [Fact]
        public void Test71_IDOR_GetAnotherUserRecord_Blocked()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            Assert.False(auth.CanAccessRecord(102));
            Assert.False(auth.CanAccessRecord("102"));
        }

        [Fact]
        public void Test72_IDOR_PutAnotherUserRecord_Blocked()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanEditQuotation(QuotationApprovalStatuses.Draft, 102));
            Assert.False(workflow.CanEditSalesOrder(SalesOrderStatuses.Draft, "102"));
            Assert.False(workflow.CanEditProformaInvoice(ProformaInvoiceStatuses.Draft, "102"));
        }

        [Fact]
        public void Test73_IDOR_DeleteAnotherUserRecord_Blocked()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanDeleteQuotation(QuotationApprovalStatuses.Draft, 102));
        }

        [Fact]
        public void Test74_IDOR_ApproveAnotherUserRecord_Blocked()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanApproveQuotation(QuotationApprovalStatuses.Submitted, 102));
            Assert.False(workflow.CanApproveDiscountApproval(DiscountApprovalStatuses.Pending, 102));
        }

        [Fact]
        public void Test75_IDOR_ConfirmAnotherUserOrder_Blocked()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanConfirmSalesOrder(SalesOrderStatuses.Submitted, "102"));
        }

        [Fact]
        public void Test76_IDOR_ConvertAnotherUserProforma_Blocked()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanConvertProformaInvoice(ProformaInvoiceStatuses.Approved, false));
        }

        [Fact]
        public void Test77_IDOR_StatusBypass_Blocked()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanApproveQuotation(QuotationApprovalStatuses.Draft, 101));
            Assert.False(workflow.CanConfirmSalesOrder(SalesOrderStatuses.Draft, "101"));
        }

        // =========================================================================
        // SECTION J: Payload Tampering (Tests 78–84)
        // =========================================================================

        [Fact]
        public void Test78_PayloadTampering_OwnerId_Rejected()
        {
            var (user, auth, _) = CreateContext(101, "Sales Executive");
            int tamperedOwnerId = 999;
            Assert.False(auth.CanAccessRecord(tamperedOwnerId));
        }

        [Fact]
        public void Test79_PayloadTampering_UserId_Rejected()
        {
            var (user, auth, _) = CreateContext(101, "Sales Executive");
            string tamperedUserId = "999";
            Assert.False(auth.CanAccessRecord(tamperedUserId));
        }

        [Fact]
        public void Test80_PayloadTampering_CreatedBy_Rejected()
        {
            var (user, auth, _) = CreateContext(101, "Sales Executive");
            string tamperedCreatedBy = "user999";
            Assert.False(auth.CanAccessRecord(tamperedCreatedBy));
        }

        [Fact]
        public void Test81_PayloadTampering_SalesPersonUserId_Rejected()
        {
            var (user, auth, _) = CreateContext(101, "Sales Executive");
            int tamperedSalesPersonId = 999;
            Assert.False(auth.CanAccessRecord(tamperedSalesPersonId));
        }

        [Fact]
        public void Test82_PayloadTampering_DirectApprovedStatusOnQuotation_Rejected()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            // Sales Executive cannot edit quotation once Approved
            Assert.False(workflow.CanEditQuotation(QuotationApprovalStatuses.Approved, 101));
        }

        [Fact]
        public void Test83_PayloadTampering_DirectApprovedStatusOnDiscount_Rejected()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            // Sales Executive cannot approve discount
            Assert.False(workflow.CanApproveDiscountApproval(DiscountApprovalStatuses.Pending, 101));
        }

        [Fact]
        public void Test84_PayloadTampering_DirectReceivedStatusOnAdvancePayment_Rejected()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            // Sales Executive cannot mark advance payment received
            Assert.False(workflow.CanReceiveAdvancePayment(AdvancePaymentStatuses.FinanceVerification));
        }

        // =========================================================================
        // SECTION K: Admin Regression (Tests 85–88+)
        // =========================================================================

        [Fact]
        public void Test85_Admin_CanAccessAllRecords()
        {
            var (user, auth, _) = CreateContext(1, "Admin");
            Assert.True(user.IsAdmin);
            Assert.Equal(AccessScope.All, user.Scope);
            Assert.True(auth.CanAccessRecord(101));
            Assert.True(auth.CanAccessRecord(102));
            Assert.True(auth.CanAccessRecord("anyUser"));
        }

        [Fact]
        public void Test86_Admin_CanPerformAllApprovalOperations()
        {
            var (_, _, workflow) = CreateContext(1, "Admin");
            Assert.True(workflow.CanApproveQuotation(QuotationApprovalStatuses.UnderReview, 101));
            Assert.True(workflow.CanApproveDiscountApproval(DiscountApprovalStatuses.Pending, 101));
            Assert.True(workflow.CanApproveProformaInvoice(ProformaInvoiceStatuses.Submitted, "101"));
            Assert.True(workflow.CanConfirmSalesOrder(SalesOrderStatuses.Submitted, "101"));
            Assert.True(workflow.CanReceiveAdvancePayment(AdvancePaymentStatuses.Submitted));
        }

        [Fact]
        public void Test87_Admin_CanPerformAllValidWorkflowTransitions()
        {
            var (_, _, workflow) = CreateContext(1, "Admin");
            Assert.True(workflow.CanProcessSalesOrder(SalesOrderStatuses.Confirmed, "101"));
            Assert.True(workflow.CanCompleteSalesOrder(SalesOrderStatuses.Processing, "101"));
            Assert.True(workflow.CanConvertProformaInvoice(ProformaInvoiceStatuses.Approved, false));
            Assert.True(workflow.CanReopenQuotation(QuotationApprovalStatuses.Cancelled, 101));
        }

        [Fact]
        public void Test88_Admin_CanPerformAllCrossModuleOperations()
        {
            var (_, _, workflow) = CreateContext(1, "Admin");
            Assert.True(workflow.CanCreateSalesOrderFromQuotation(QuotationApprovalStatuses.Approved, 101));
            Assert.True(workflow.CanCreateProformaFromSalesOrder(SalesOrderStatuses.Draft, "101"));
            Assert.True(workflow.CanApplyAdvancePayment(AdvancePaymentStatuses.Received, 10000m, "101"));
        }
    }
}
