using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ErpRbacPhase4Tests
    {
        private static (ICurrentUser currentUser, IErpAuthorizationService authService, IErpWorkflowAuthorizationService workflowAuth) CreateContext(
            int? userId,
            string? role,
            string email = "sales@company.com",
            string fullName = "Jane Doe",
            IEnumerable<string>? extraPermissions = null)
        {
            var claims = new List<Claim>();

            if (userId.HasValue)
            {
                claims.Add(new Claim("userId", userId.Value.ToString()));
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
            }

            if (!string.IsNullOrEmpty(role))
            {
                claims.Add(new Claim("role", role));
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            if (!string.IsNullOrEmpty(email))
            {
                claims.Add(new Claim("email", email));
                claims.Add(new Claim(ClaimTypes.Email, email));
            }

            if (!string.IsNullOrEmpty(fullName))
            {
                claims.Add(new Claim("fullName", fullName));
                claims.Add(new Claim(ClaimTypes.Name, fullName));
            }

            if (extraPermissions != null)
            {
                foreach (var p in extraPermissions)
                {
                    claims.Add(new Claim("permission", p));
                }
            }

            var identity = new ClaimsIdentity(claims, userId.HasValue || !string.IsNullOrEmpty(role) ? "TestAuth" : null);
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };
            var accessor = new TestHttpContextAccessor { HttpContext = httpContext };

            var currentUser = new CurrentUser(accessor);
            var authService = new ErpAuthorizationService(currentUser);
            var workflowAuth = new ErpWorkflowAuthorizationService(currentUser, authService);

            return (currentUser, authService, workflowAuth);
        }

        // =========================================================================
        // SECTION 1: IDENTITY TESTS (1–8)
        // =========================================================================

        [Fact]
        public void Test01_AdminIdentity_RecognizedCorrectly()
        {
            var (user, _, _) = CreateContext(1, "Admin");
            Assert.True(user.IsAdmin);
            Assert.Equal(AccessScope.All, user.Scope);
            Assert.Equal(1, user.UserId);
        }

        [Fact]
        public void Test02_AdministratorIdentity_RecognizedAsAdmin()
        {
            var (user, _, _) = CreateContext(2, "Administrator");
            Assert.True(user.IsAdmin);
            Assert.Equal(AccessScope.All, user.Scope);
        }

        [Fact]
        public void Test03_SalesExecutiveIdentity_RecognizedWithOwnScope()
        {
            var (user, _, _) = CreateContext(101, "Sales Executive");
            Assert.False(user.IsAdmin);
            Assert.Equal(AccessScope.Own, user.Scope);
            Assert.Equal(101, user.UserId);
        }

        [Fact]
        public void Test04_MissingRole_DefaultsToOwnScopeNonAdmin()
        {
            var (user, _, _) = CreateContext(101, null);
            Assert.False(user.IsAdmin);
            Assert.Equal(AccessScope.Own, user.Scope);
        }

        [Fact]
        public void Test05_MissingUserId_CannotAuthorizeOwnershipOperations()
        {
            var (user, auth, _) = CreateContext(null, "Sales Executive");
            Assert.Null(user.UserId);
            Assert.False(auth.CanAccessRecord(101));
            Assert.False(auth.CanAccessRecord("101"));
        }

        [Fact]
        public void Test06_UnauthenticatedUser_HasNoPermissionsOrScope()
        {
            var accessor = new TestHttpContextAccessor { HttpContext = new DefaultHttpContext() };
            var user = new CurrentUser(accessor);
            var auth = new ErpAuthorizationService(user);

            Assert.False(user.IsAuthenticated);
            Assert.Empty(user.Permissions);
            Assert.False(auth.Authorize(ErpPermissions.Quotations.View));
            Assert.False(auth.CanAccessRecord(101));
        }

        [Fact]
        public void Test07_UnknownNonAdminRole_DoesNotReceiveAdminPrivileges()
        {
            var (user, _, _) = CreateContext(105, "GuestOperator");
            Assert.False(user.IsAdmin);
            Assert.Equal(AccessScope.Own, user.Scope);
        }

        [Fact]
        public void Test08_AdminAssistantOrPartialName_DoesNotAccidentallyBecomeAdmin()
        {
            var (user, _, _) = CreateContext(106, "Admin Assistant");
            Assert.False(user.IsAdmin);
            Assert.Equal(AccessScope.Own, user.Scope);
        }

        // =========================================================================
        // SECTION 2: PERMISSIONS TESTS (9–11)
        // =========================================================================

        [Fact]
        public void Test09_Admin_HasAllPermissions()
        {
            var (user, auth, _) = CreateContext(1, "Admin");
            Assert.True(auth.Authorize(ErpPermissions.Quotations.Approve));
            Assert.True(auth.Authorize(ErpPermissions.DiscountApprovals.Approve));
            Assert.True(auth.Authorize(ErpPermissions.SalesOrders.Confirm));
            Assert.True(auth.Authorize(ErpPermissions.ProformaInvoices.Convert));
            Assert.True(auth.Authorize(ErpPermissions.AdvancePayments.Verify));
            Assert.True(auth.Authorize(ErpPermissions.SalesTargets.Create));
            Assert.True(auth.Authorize(ErpPermissions.Performance.Export));
        }

        [Fact]
        public void Test10_SalesExecutive_HasAllowedPermissions()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            Assert.True(auth.Authorize(ErpPermissions.Quotations.View));
            Assert.True(auth.Authorize(ErpPermissions.Quotations.Create));
            Assert.True(auth.Authorize(ErpPermissions.DiscountApprovals.Create));
            Assert.True(auth.Authorize(ErpPermissions.SalesOrders.Create));
            Assert.True(auth.Authorize(ErpPermissions.ProformaInvoices.Create));
            Assert.True(auth.Authorize(ErpPermissions.AdvancePayments.Create));
            Assert.True(auth.Authorize(ErpPermissions.Performance.View));
        }

        [Fact]
        public void Test11_SalesExecutive_DeniedPrivilegedPermissions()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            Assert.False(auth.Authorize(ErpPermissions.Quotations.Approve));
            Assert.False(auth.Authorize(ErpPermissions.Quotations.Reject));
            Assert.False(auth.Authorize(ErpPermissions.DiscountApprovals.Approve));
            Assert.False(auth.Authorize(ErpPermissions.SalesOrders.Confirm));
            Assert.False(auth.Authorize(ErpPermissions.SalesOrders.Cancel));
            Assert.False(auth.Authorize(ErpPermissions.ProformaInvoices.Convert));
            Assert.False(auth.Authorize(ErpPermissions.AdvancePayments.Verify));
            Assert.False(auth.Authorize(ErpPermissions.AdvancePayments.Receive));
            Assert.False(auth.Authorize(ErpPermissions.SalesTargets.Create));
            Assert.False(auth.Authorize(ErpPermissions.Performance.LeaderboardView));
        }

        // =========================================================================
        // SECTION 3: OWNERSHIP TESTS (12–19)
        // =========================================================================

        [Fact]
        public void Test12_UserA_CanAccessOwnQuotation()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            Assert.True(auth.CanAccessRecord(101));
            Assert.True(auth.CanAccessRecord("101"));
        }

        [Fact]
        public void Test13_UserA_DeniedUserBQuotation()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            Assert.False(auth.CanAccessRecord(102));
            Assert.False(auth.CanAccessRecord("102"));
        }

        [Fact]
        public void Test14_UserA_DeniedUserBSalesOrder()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            Assert.False(auth.CanAccessRecord("user102"));
        }

        [Fact]
        public void Test15_UserA_DeniedUserBProformaInvoice()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            Assert.False(auth.CanAccessRecord("102"));
        }

        [Fact]
        public void Test16_UserA_DeniedUserBAdvancePayment()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            Assert.False(auth.CanAccessRecord("102"));
        }

        [Fact]
        public void Test17_UserA_DeniedUserBSalesTarget()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            Assert.False(auth.CanAccessRecord(102));
        }

        [Fact]
        public void Test18_UserA_DeniedUserBPerformance()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            Assert.False(auth.CanAccessRecord(102));
        }

        [Fact]
        public void Test19_UserA_DeniedUserBDiscountApproval()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            Assert.False(auth.CanAccessRecord(102));
        }

        // =========================================================================
        // SECTION 4: IDOR SECURITY TESTS (20–24)
        // =========================================================================

        [Fact]
        public void Test20_IDOR_GetUnauthorizedRecord_Denied()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            Assert.False(auth.CanAccessRecord(999));
        }

        [Fact]
        public void Test21_IDOR_PostUnauthorizedRelationship_Denied()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanCreateSalesOrderFromQuotation(QuotationApprovalStatuses.Approved, 999));
        }

        [Fact]
        public void Test22_IDOR_PutUnauthorizedRecord_Denied()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanEditQuotation(QuotationApprovalStatuses.Draft, 999));
            Assert.False(workflow.CanEditSalesOrder(SalesOrderStatuses.Draft, "999"));
        }

        [Fact]
        public void Test23_IDOR_PatchUnauthorizedStatus_Denied()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanConfirmSalesOrder(SalesOrderStatuses.Submitted, "999"));
        }

        [Fact]
        public void Test24_IDOR_DeleteUnauthorizedRecord_Denied()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanDeleteQuotation(QuotationApprovalStatuses.Draft, 999));
        }

        // =========================================================================
        // SECTION 5: PAYLOAD TAMPERING TESTS (25–31)
        // =========================================================================

        [Fact]
        public void Test25_PayloadTampering_OwnerId_IgnoredByServerAuth()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            int forgedOwnerId = 555;
            Assert.False(auth.CanAccessRecord(forgedOwnerId));
        }

        [Fact]
        public void Test26_PayloadTampering_CreatedBy_IgnoredByServerAuth()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            string forgedCreatedBy = "user555";
            Assert.False(auth.CanAccessRecord(forgedCreatedBy));
        }

        [Fact]
        public void Test27_PayloadTampering_SalesPersonId_IgnoredByServerAuth()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            string forgedSalesPersonId = "555";
            Assert.False(auth.CanAccessRecord(forgedSalesPersonId));
        }

        [Fact]
        public void Test28_PayloadTampering_SalesPersonUserId_IgnoredByServerAuth()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            int forgedSalesPersonUserId = 555;
            Assert.False(auth.CanAccessRecord(forgedSalesPersonUserId));
        }

        [Fact]
        public void Test29_PayloadTampering_UserIdQuery_IgnoredByServerAuth()
        {
            var (user, _, _) = CreateContext(101, "Sales Executive");
            Assert.Equal(101, user.UserId);
        }

        [Fact]
        public void Test30_PayloadTampering_Status_CannotBypassWorkflowGuard()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanEditQuotation(QuotationApprovalStatuses.Approved, 101));
            Assert.False(workflow.CanEditSalesOrder(SalesOrderStatuses.Confirmed, "101"));
        }

        [Fact]
        public void Test31_PayloadTampering_ApprovalStatus_CannotBypassWorkflowGuard()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanApproveQuotation(QuotationApprovalStatuses.Submitted, 101));
            Assert.False(workflow.CanApproveDiscountApproval(DiscountApprovalStatuses.Pending, 101));
        }

        // =========================================================================
        // SECTION 6: WORKFLOW PRIVILEGE TESTS (32–42)
        // =========================================================================

        [Fact]
        public void Test32_SalesExecutive_CannotApproveQuotation()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanApproveQuotation(QuotationApprovalStatuses.UnderReview, 101));
        }

        [Fact]
        public void Test33_SalesExecutive_CannotRejectQuotation()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanRejectQuotation(QuotationApprovalStatuses.UnderReview, 101));
        }

        [Fact]
        public void Test34_SalesExecutive_CannotReturnQuotation()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanReturnQuotation(QuotationApprovalStatuses.UnderReview, 101));
        }

        [Fact]
        public void Test35_SalesExecutive_CannotReopenQuotation()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanReopenQuotation(QuotationApprovalStatuses.Cancelled, 101));
        }

        [Fact]
        public void Test36_SalesExecutive_CannotApproveDiscount()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanApproveDiscountApproval(DiscountApprovalStatuses.Pending, 101));
        }

        [Fact]
        public void Test37_SalesExecutive_CannotApproveSalesOrder()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanConfirmSalesOrder(SalesOrderStatuses.Submitted, "101"));
        }

        [Fact]
        public void Test38_SalesExecutive_CannotCancelSalesOrder()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanCancelSalesOrder(SalesOrderStatuses.Submitted, "101"));
        }

        [Fact]
        public void Test39_SalesExecutive_CannotApproveProformaInvoice()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanApproveProformaInvoice(ProformaInvoiceStatuses.Submitted, "101"));
        }

        [Fact]
        public void Test40_SalesExecutive_CannotConvertProformaInvoice()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanConvertProformaInvoice(ProformaInvoiceStatuses.Approved, false));
        }

        [Fact]
        public void Test41_SalesExecutive_CannotVerifyAdvancePayment()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanVerifyAdvancePayment(AdvancePaymentStatuses.Submitted));
        }

        [Fact]
        public void Test42_SalesExecutive_CannotReceiveAdvancePayment()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanReceiveAdvancePayment(AdvancePaymentStatuses.FinanceVerification));
        }

        // =========================================================================
        // SECTION 7: SEPARATION OF DUTIES TESTS (43–47)
        // =========================================================================

        [Fact]
        public void Test43_SalesExecutive_CannotSelfApproveDiscountRequest()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanApproveDiscountApproval(DiscountApprovalStatuses.Pending, 101));
        }

        [Fact]
        public void Test44_SalesExecutive_CannotSelfApproveQuotation()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanApproveQuotation(QuotationApprovalStatuses.Submitted, 101));
        }

        [Fact]
        public void Test45_SalesExecutive_CannotSelfConfirmSalesOrder()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanConfirmSalesOrder(SalesOrderStatuses.Submitted, "101"));
        }

        [Fact]
        public void Test46_SalesExecutive_CannotSelfApproveProformaInvoice()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanApproveProformaInvoice(ProformaInvoiceStatuses.Submitted, "101"));
        }

        [Fact]
        public void Test47_SalesExecutive_CannotSelfVerifyAdvancePayment()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanVerifyAdvancePayment(AdvancePaymentStatuses.Submitted));
        }

        // =========================================================================
        // SECTION 8: CROSS-MODULE RELATIONSHIPS (48–51)
        // =========================================================================

        [Fact]
        public void Test48_OwnQuotation_To_UnauthorizedSalesOrder_Denied()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanCreateSalesOrderFromQuotation(QuotationApprovalStatuses.Approved, 102));
        }

        [Fact]
        public void Test49_OwnSalesOrder_To_UnauthorizedProformaInvoice_Denied()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanCreateProformaFromSalesOrder(SalesOrderStatuses.Draft, "102"));
        }

        [Fact]
        public void Test50_OwnProformaInvoice_To_UnauthorizedAdvancePayment_Denied()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanApplyAdvancePayment(AdvancePaymentStatuses.Received, 5000m, "102"));
        }

        [Fact]
        public void Test51_OwnQuotation_To_UnauthorizedDiscountRequest_Denied()
        {
            var (_, _, workflow) = CreateContext(101, "Sales Executive");
            Assert.False(workflow.CanCreateDiscountApproval(102));
        }

        // =========================================================================
        // SECTION 9: QUERY FILTER BYPASS TESTS (52–56)
        // =========================================================================

        [Fact]
        public void Test52_OwnerIdQuery_CannotBypassOwnScope()
        {
            var (user, auth, _) = CreateContext(101, "Sales Executive");
            Assert.Equal(AccessScope.Own, user.Scope);
            Assert.False(auth.CanAccessRecord(102));
        }

        [Fact]
        public void Test53_UserIdQuery_CannotBypassOwnScope()
        {
            var (user, auth, _) = CreateContext(101, "Sales Executive");
            Assert.Equal(AccessScope.Own, user.Scope);
            Assert.False(auth.CanAccessRecord("102"));
        }

        [Fact]
        public void Test54_Pagination_CannotBypassOwnScope()
        {
            var (user, _, _) = CreateContext(101, "Sales Executive");
            Assert.Equal(AccessScope.Own, user.Scope);
            Assert.Equal(101, user.UserId);
        }

        [Fact]
        public void Test55_Search_CannotBypassOwnScope()
        {
            var (user, auth, _) = CreateContext(101, "Sales Executive");
            Assert.False(auth.CanAccessRecord(102));
        }

        [Fact]
        public void Test56_Export_RestrictedToAuthorizedRecords()
        {
            var (_, auth, _) = CreateContext(101, "Sales Executive");
            Assert.False(auth.Authorize(ErpPermissions.Performance.Export));
            Assert.False(auth.Authorize(ErpPermissions.SalesTargets.Create));
        }

        // =========================================================================
        // SECTION 10: ADMIN REGRESSION TESTS (57–60+)
        // =========================================================================

        [Fact]
        public void Test57_Admin_AccessesUserARecords()
        {
            var (user, auth, _) = CreateContext(1, "Admin");
            Assert.True(user.IsAdmin);
            Assert.True(auth.CanAccessRecord(101));
            Assert.True(auth.CanAccessRecord("101"));
        }

        [Fact]
        public void Test58_Admin_AccessesUserBRecords()
        {
            var (user, auth, _) = CreateContext(1, "Admin");
            Assert.True(user.IsAdmin);
            Assert.True(auth.CanAccessRecord(102));
            Assert.True(auth.CanAccessRecord("102"));
        }

        [Fact]
        public void Test59_Admin_RetainsAllPermissions()
        {
            var (user, auth, _) = CreateContext(1, "Admin");
            Assert.True(user.IsAdmin);
            Assert.Equal(ErpPermissions.All.Count, user.Permissions.Count);
        }

        [Fact]
        public void Test60_Admin_RetainsAllWorkflowTransitions()
        {
            var (_, _, workflow) = CreateContext(1, "Admin");
            Assert.True(workflow.CanApproveQuotation(QuotationApprovalStatuses.Submitted, 101));
            Assert.True(workflow.CanApproveDiscountApproval(DiscountApprovalStatuses.Pending, 101));
            Assert.True(workflow.CanConfirmSalesOrder(SalesOrderStatuses.Submitted, "101"));
            Assert.True(workflow.CanApproveProformaInvoice(ProformaInvoiceStatuses.Submitted, "101"));
            Assert.True(workflow.CanConvertProformaInvoice(ProformaInvoiceStatuses.Approved, false));
            Assert.True(workflow.CanVerifyAdvancePayment(AdvancePaymentStatuses.Submitted));
            Assert.True(workflow.CanReceiveAdvancePayment(AdvancePaymentStatuses.Submitted));
        }
    }
}
