using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using ERP.Application.Common.Security;
using ERP.Domain.Enums;
using ERP.Infrastructure.Security;
using ERP.Shared.Security;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Backend_ERP.Tests
{
    public class TestHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }

    public class ErpRbacPhase2Tests
    {
        private static IHttpContextAccessor CreateAccessorWithClaims(IEnumerable<Claim> claims)
        {
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };
            return new TestHttpContextAccessor { HttpContext = httpContext };
        }

        private static (ICurrentUser currentUser, IErpAuthorizationService authService) CreateContext(
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

            var accessor = CreateAccessorWithClaims(claims);
            var currentUser = new CurrentUser(accessor);
            var authService = new ErpAuthorizationService(currentUser);
            return (currentUser, authService);
        }

        // =========================================================================
        // SECTION 1: Sales Executive Role Recognition & Scope (Tests 1-8)
        // =========================================================================

        [Fact]
        public void Test01_SalesExecutiveRole_MapsToNonAdmin()
        {
            var (user, _) = CreateContext(101, "Sales Executive");
            Assert.False(user.IsAdmin);
        }

        [Fact]
        public void Test02_SalesExecutiveRole_MapsToOwnScope()
        {
            var (user, _) = CreateContext(101, "Sales Executive");
            Assert.Equal(AccessScope.Own, user.Scope);
        }

        [Fact]
        public void Test03_SalesExecutiveRole_ExtractsUserId()
        {
            var (user, _) = CreateContext(101, "Sales Executive");
            Assert.Equal(101, user.UserId);
        }

        [Fact]
        public void Test04_SalesExecutiveRole_ExtractsEmailAndFullName()
        {
            var (user, _) = CreateContext(101, "Sales Executive", "jane@company.com", "Jane Doe");
            Assert.Equal("jane@company.com", user.Email);
            Assert.Equal("Jane Doe", user.FullName);
        }

        [Fact]
        public void Test05_SalesExecutiveRole_IsAuthenticated()
        {
            var (user, _) = CreateContext(101, "Sales Executive");
            Assert.True(user.IsAuthenticated);
        }

        [Theory]
        [InlineData("Sales Executive")]
        [InlineData("sales executive")]
        [InlineData("Salesperson")]
        [InlineData("User")]
        [InlineData("Sales")]
        public void Test06_VariousNonAdminRoles_ResolveToOwnScope(string role)
        {
            var (user, _) = CreateContext(101, role);
            Assert.False(user.IsAdmin);
            Assert.Equal(AccessScope.Own, user.Scope);
        }

        [Fact]
        public void Test07_AdminRole_ResolvesToAllScope()
        {
            var (user, _) = CreateContext(1, "Admin");
            Assert.True(user.IsAdmin);
            Assert.Equal(AccessScope.All, user.Scope);
        }

        [Fact]
        public void Test08_AdministratorRole_ResolvesToAllScope()
        {
            var (user, _) = CreateContext(1, "Administrator");
            Assert.True(user.IsAdmin);
            Assert.Equal(AccessScope.All, user.Scope);
        }

        // =========================================================================
        // SECTION 2: Sales Executive Price List Permissions (Tests 9-13)
        // =========================================================================

        [Fact]
        public void Test09_SalesExecutive_CanViewPriceLists()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.True(auth.Authorize(ErpPermissions.PriceLists.View));
        }

        [Fact]
        public void Test10_SalesExecutive_CanComparePriceLists()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.True(auth.Authorize(ErpPermissions.PriceLists.Compare));
        }

        [Fact]
        public void Test11_SalesExecutive_CannotCreatePriceLists()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.False(auth.Authorize(ErpPermissions.PriceLists.Create));
        }

        [Fact]
        public void Test12_SalesExecutive_CannotEditOrDeletePriceLists()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.False(auth.Authorize(ErpPermissions.PriceLists.Edit));
            Assert.False(auth.Authorize(ErpPermissions.PriceLists.Delete));
        }

        [Fact]
        public void Test13_SalesExecutive_CannotActivateOrExportPriceLists()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.False(auth.Authorize(ErpPermissions.PriceLists.Activate));
            Assert.False(auth.Authorize(ErpPermissions.PriceLists.Export));
        }

        // =========================================================================
        // SECTION 3: Sales Executive Quotations Permissions (Tests 14-19)
        // =========================================================================

        [Fact]
        public void Test14_SalesExecutive_CanViewQuotations()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.True(auth.Authorize(ErpPermissions.Quotations.View));
        }

        [Fact]
        public void Test15_SalesExecutive_CanCreateEditDuplicateQuotations()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.True(auth.Authorize(ErpPermissions.Quotations.Create));
            Assert.True(auth.Authorize(ErpPermissions.Quotations.Edit));
            Assert.True(auth.Authorize(ErpPermissions.Quotations.Duplicate));
        }

        [Fact]
        public void Test16_SalesExecutive_CanSubmitQuotations()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.True(auth.Authorize(ErpPermissions.Quotations.Submit));
        }

        [Fact]
        public void Test17_SalesExecutive_CanGeneratePdfAndSendEmailForQuotations()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.True(auth.Authorize(ErpPermissions.Quotations.GeneratePdf));
            Assert.True(auth.Authorize(ErpPermissions.Quotations.SendEmail));
            Assert.True(auth.Authorize(ErpPermissions.Quotations.Convert));
        }

        [Fact]
        public void Test18_SalesExecutive_CannotDeleteQuotations()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.False(auth.Authorize(ErpPermissions.Quotations.Delete));
        }

        [Fact]
        public void Test19_SalesExecutive_CannotApproveRejectReturnOrReopenQuotations()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.False(auth.Authorize(ErpPermissions.Quotations.Approve));
            Assert.False(auth.Authorize(ErpPermissions.Quotations.Reject));
            Assert.False(auth.Authorize(ErpPermissions.Quotations.Return));
            Assert.False(auth.Authorize(ErpPermissions.Quotations.Reopen));
        }

        // =========================================================================
        // SECTION 4: Sales Executive Discount Approvals Permissions (Tests 20-23)
        // =========================================================================

        [Fact]
        public void Test20_SalesExecutive_CanViewDiscountApprovals()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.True(auth.Authorize(ErpPermissions.DiscountApprovals.View));
        }

        [Fact]
        public void Test21_SalesExecutive_CanCreateAndResubmitDiscountApprovals()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.True(auth.Authorize(ErpPermissions.DiscountApprovals.Create));
            Assert.True(auth.Authorize(ErpPermissions.DiscountApprovals.Resubmit));
        }

        [Fact]
        public void Test22_SalesExecutive_CannotApproveOrRejectDiscountApprovals()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.False(auth.Authorize(ErpPermissions.DiscountApprovals.Approve));
            Assert.False(auth.Authorize(ErpPermissions.DiscountApprovals.Reject));
        }

        [Fact]
        public void Test23_SalesExecutive_CannotReturnOrCancelDiscountApprovals()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.False(auth.Authorize(ErpPermissions.DiscountApprovals.Return));
            Assert.False(auth.Authorize(ErpPermissions.DiscountApprovals.Cancel));
        }

        // =========================================================================
        // SECTION 5: Sales Executive Sales Orders Permissions (Tests 24-28)
        // =========================================================================

        [Fact]
        public void Test24_SalesExecutive_CanViewSalesOrders()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.True(auth.Authorize(ErpPermissions.SalesOrders.View));
        }

        [Fact]
        public void Test25_SalesExecutive_CanCreateEditSubmitSalesOrders()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.True(auth.Authorize(ErpPermissions.SalesOrders.Create));
            Assert.True(auth.Authorize(ErpPermissions.SalesOrders.Edit));
            Assert.True(auth.Authorize(ErpPermissions.SalesOrders.Submit));
        }

        [Fact]
        public void Test26_SalesExecutive_CanGeneratePdfAndEmailAndAuditViewSalesOrders()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.True(auth.Authorize(ErpPermissions.SalesOrders.GeneratePdf));
            Assert.True(auth.Authorize(ErpPermissions.SalesOrders.SendEmail));
            Assert.True(auth.Authorize(ErpPermissions.SalesOrders.AuditView));
        }

        [Fact]
        public void Test27_SalesExecutive_CannotConfirmOrProcessSalesOrders()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.False(auth.Authorize(ErpPermissions.SalesOrders.Confirm));
            Assert.False(auth.Authorize(ErpPermissions.SalesOrders.Process));
        }

        [Fact]
        public void Test28_SalesExecutive_CannotCompleteOrCancelSalesOrders()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.False(auth.Authorize(ErpPermissions.SalesOrders.Complete));
            Assert.False(auth.Authorize(ErpPermissions.SalesOrders.Cancel));
        }

        // =========================================================================
        // SECTION 6: Sales Executive Proforma Invoices Permissions (Tests 29-33)
        // =========================================================================

        [Fact]
        public void Test29_SalesExecutive_CanViewProformaInvoices()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.True(auth.Authorize(ErpPermissions.ProformaInvoices.View));
        }

        [Fact]
        public void Test30_SalesExecutive_CanCreateEditSubmitProformaInvoices()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.True(auth.Authorize(ErpPermissions.ProformaInvoices.Create));
            Assert.True(auth.Authorize(ErpPermissions.ProformaInvoices.Edit));
            Assert.True(auth.Authorize(ErpPermissions.ProformaInvoices.Submit));
        }

        [Fact]
        public void Test31_SalesExecutive_CanSendEmailWhatsappAndGeneratePdfForProforma()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.True(auth.Authorize(ErpPermissions.ProformaInvoices.Email));
            Assert.True(auth.Authorize(ErpPermissions.ProformaInvoices.Whatsapp));
            Assert.True(auth.Authorize(ErpPermissions.ProformaInvoices.GeneratePdf));
        }

        [Fact]
        public void Test32_SalesExecutive_CannotApproveOrRejectProformaInvoices()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.False(auth.Authorize(ErpPermissions.ProformaInvoices.Approve));
            Assert.False(auth.Authorize(ErpPermissions.ProformaInvoices.Reject));
        }

        [Fact]
        public void Test33_SalesExecutive_CannotReturnOrConvertProformaInvoices()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.False(auth.Authorize(ErpPermissions.ProformaInvoices.Return));
            Assert.False(auth.Authorize(ErpPermissions.ProformaInvoices.Convert));
        }

        // =========================================================================
        // SECTION 7: Advance Payments, Sales Targets & Performance (Tests 34-41)
        // =========================================================================

        [Fact]
        public void Test34_SalesExecutive_CanViewAndCreateAdvancePayments()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.True(auth.Authorize(ErpPermissions.AdvancePayments.View));
            Assert.True(auth.Authorize(ErpPermissions.AdvancePayments.Create));
            Assert.True(auth.Authorize(ErpPermissions.AdvancePayments.Apply));
        }

        [Fact]
        public void Test35_SalesExecutive_CannotVerifyOrReceiveAdvancePayments()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.False(auth.Authorize(ErpPermissions.AdvancePayments.Verify));
            Assert.False(auth.Authorize(ErpPermissions.AdvancePayments.Receive));
        }

        [Fact]
        public void Test36_SalesExecutive_CanViewSalesTargetsAndDashboard()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.True(auth.Authorize(ErpPermissions.SalesTargets.View));
            Assert.True(auth.Authorize(ErpPermissions.SalesTargets.DashboardView));
        }

        [Fact]
        public void Test37_SalesExecutive_CannotCreateEditDeleteDuplicateSalesTargets()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.False(auth.Authorize(ErpPermissions.SalesTargets.Create));
            Assert.False(auth.Authorize(ErpPermissions.SalesTargets.Edit));
            Assert.False(auth.Authorize(ErpPermissions.SalesTargets.Delete));
            Assert.False(auth.Authorize(ErpPermissions.SalesTargets.Duplicate));
        }

        [Fact]
        public void Test38_SalesExecutive_CanViewPerformance()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.True(auth.Authorize(ErpPermissions.Performance.View));
        }

        [Fact]
        public void Test39_SalesExecutive_CannotViewLeaderboardsOrExportPerformance()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.False(auth.Authorize(ErpPermissions.Performance.LeaderboardView));
            Assert.False(auth.Authorize(ErpPermissions.Performance.Export));
        }

        [Fact]
        public void Test40_SalesExecutive_CanViewDispatchAndPod()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.True(auth.Authorize(ErpPermissions.DispatchLogistics.View));
            Assert.True(auth.Authorize(ErpPermissions.DispatchLogistics.Pod));
            Assert.False(auth.Authorize(ErpPermissions.DispatchLogistics.Assign));
            Assert.False(auth.Authorize(ErpPermissions.DispatchLogistics.Lr));
            Assert.False(auth.Authorize(ErpPermissions.DispatchLogistics.Eway));
        }

        [Fact]
        public void Test41_SalesExecutive_CanViewAccountingAndOutstanding()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.True(auth.Authorize(ErpPermissions.Accounting.View));
            Assert.True(auth.Authorize(ErpPermissions.Accounting.Outstanding));
        }

        // =========================================================================
        // SECTION 8: Record-Level OWN-Scope Authorization (Tests 42-50)
        // =========================================================================

        [Fact]
        public void Test42_SalesExecutive_CanAccessOwnRecord_ByUserId()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.True(auth.CanAccessRecord(101));
        }

        [Fact]
        public void Test43_SalesExecutive_CannotAccessOtherUserRecord_ByUserId()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.False(auth.CanAccessRecord(102));
        }

        [Fact]
        public void Test44_SalesExecutive_CannotAccessNullRecordOwner()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.False(auth.CanAccessRecord((int?)null));
            Assert.False(auth.CanAccessRecord((string?)null));
            Assert.False(auth.CanAccessRecord(""));
        }

        [Fact]
        public void Test45_SalesExecutive_CanAccessOwnRecord_ByStringUserId()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.True(auth.CanAccessRecord("101"));
        }

        [Fact]
        public void Test46_SalesExecutive_CannotAccessOtherUserRecord_ByStringUserId()
        {
            var (_, auth) = CreateContext(101, "Sales Executive");
            Assert.False(auth.CanAccessRecord("102"));
        }

        [Fact]
        public void Test47_SalesExecutive_CanAccessOwnRecord_ByEmail()
        {
            var (_, auth) = CreateContext(101, "Sales Executive", "sales1@example.com", "Sales Person 1");
            Assert.True(auth.CanAccessRecord("sales1@example.com"));
        }

        [Fact]
        public void Test48_SalesExecutive_CannotAccessOtherUserRecord_ByEmail()
        {
            var (_, auth) = CreateContext(101, "Sales Executive", "sales1@example.com", "Sales Person 1");
            Assert.False(auth.CanAccessRecord("sales2@example.com"));
        }

        [Fact]
        public void Test49_SalesExecutive_CanAccessOwnRecord_ByFullName()
        {
            var (_, auth) = CreateContext(101, "Sales Executive", "sales1@example.com", "Sales Person 1");
            Assert.True(auth.CanAccessRecord("Sales Person 1"));
        }

        [Fact]
        public void Test50_SalesExecutive_CannotAccessOtherUserRecord_ByFullName()
        {
            var (_, auth) = CreateContext(101, "Sales Executive", "sales1@example.com", "Sales Person 1");
            Assert.False(auth.CanAccessRecord("Sales Person 2"));
        }

        // =========================================================================
        // SECTION 9: Multi-User Cross-Testing & Isolation (Tests 51-54)
        // =========================================================================

        [Fact]
        public void Test51_CrossUserIsolation_UserA_CannotAccess_UserB_Quotation()
        {
            var (_, authA) = CreateContext(101, "Sales Executive", "sales1@company.com", "Sales Person 1");
            var (_, authB) = CreateContext(102, "Sales Executive", "sales2@company.com", "Sales Person 2");

            int userAQuotationOwnerId = 101;
            int userBQuotationOwnerId = 102;

            // User A can access own, but not User B
            Assert.True(authA.CanAccessRecord(userAQuotationOwnerId));
            Assert.False(authA.CanAccessRecord(userBQuotationOwnerId));

            // User B can access own, but not User A
            Assert.True(authB.CanAccessRecord(userBQuotationOwnerId));
            Assert.False(authB.CanAccessRecord(userAQuotationOwnerId));
        }

        [Fact]
        public void Test52_CrossUserIsolation_UserA_CannotAccess_UserB_SalesOrder()
        {
            var (_, authA) = CreateContext(101, "Sales Executive", "sales1@company.com", "Sales Person 1");
            var (_, authB) = CreateContext(102, "Sales Executive", "sales2@company.com", "Sales Person 2");

            string userACreatedBy = "101";
            string userBCreatedBy = "102";

            Assert.True(authA.CanAccessRecord(userACreatedBy));
            Assert.False(authA.CanAccessRecord(userBCreatedBy));

            Assert.True(authB.CanAccessRecord(userBCreatedBy));
            Assert.False(authB.CanAccessRecord(userACreatedBy));
        }

        [Fact]
        public void Test53_CrossUserIsolation_UserA_CannotAccess_UserB_ProformaInvoice()
        {
            var (_, authA) = CreateContext(101, "Sales Executive", "sales1@company.com", "Sales Person 1");
            var (_, authB) = CreateContext(102, "Sales Executive", "sales2@company.com", "Sales Person 2");

            int userAPiSalesPersonUserId = 101;
            int userBPiSalesPersonUserId = 102;

            Assert.True(authA.CanAccessRecord(userAPiSalesPersonUserId));
            Assert.False(authA.CanAccessRecord(userBPiSalesPersonUserId));

            Assert.True(authB.CanAccessRecord(userBPiSalesPersonUserId));
            Assert.False(authB.CanAccessRecord(userAPiSalesPersonUserId));
        }

        [Fact]
        public void Test54_CrossUserIsolation_UserA_CannotAccess_UserB_DiscountApproval()
        {
            var (_, authA) = CreateContext(101, "Sales Executive", "sales1@company.com", "Sales Person 1");
            var (_, authB) = CreateContext(102, "Sales Executive", "sales2@company.com", "Sales Person 2");

            int userADiscountOwner = 101;
            int userBDiscountOwner = 102;

            Assert.True(authA.CanAccessRecord(userADiscountOwner));
            Assert.False(authA.CanAccessRecord(userBDiscountOwner));

            Assert.True(authB.CanAccessRecord(userBDiscountOwner));
            Assert.False(authB.CanAccessRecord(userADiscountOwner));
        }

        // =========================================================================
        // SECTION 10: Admin ALL-Scope Preservation & Regression Checks (Tests 55-58)
        // =========================================================================

        [Fact]
        public void Test55_Admin_RetainsAllPermissions()
        {
            var (user, auth) = CreateContext(1, "Admin");
            Assert.True(user.IsAdmin);
            Assert.Equal(AccessScope.All, user.Scope);

            foreach (var perm in ErpPermissions.All)
            {
                Assert.True(auth.Authorize(perm));
            }
        }

        [Fact]
        public void Test56_Admin_CanAccessAnyRecordOwner_ByUserId()
        {
            var (_, auth) = CreateContext(1, "Admin");

            Assert.True(auth.CanAccessRecord(101));
            Assert.True(auth.CanAccessRecord(102));
            Assert.True(auth.CanAccessRecord(999));
            Assert.True(auth.CanAccessRecord((int?)null));
        }

        [Fact]
        public void Test57_Admin_CanAccessAnyRecordOwner_ByString()
        {
            var (_, auth) = CreateContext(1, "Admin");

            Assert.True(auth.CanAccessRecord("101"));
            Assert.True(auth.CanAccessRecord("102"));
            Assert.True(auth.CanAccessRecord("sales1@example.com"));
            Assert.True(auth.CanAccessRecord("Unknown User"));
            Assert.True(auth.CanAccessRecord((string?)null));
        }

        [Fact]
        public void Test58_Admin_CanAuthorizeAnyCombo()
        {
            var (_, auth) = CreateContext(1, "Admin");

            Assert.True(auth.AuthorizeAny(ErpPermissions.Quotations.Approve, ErpPermissions.Quotations.Reject));
            Assert.True(auth.AuthorizeAny(ErpPermissions.DiscountApprovals.Approve, ErpPermissions.DiscountApprovals.Cancel));
            Assert.True(auth.AuthorizeAny(ErpPermissions.SalesOrders.Confirm, ErpPermissions.SalesOrders.Complete));
        }
    }
}
