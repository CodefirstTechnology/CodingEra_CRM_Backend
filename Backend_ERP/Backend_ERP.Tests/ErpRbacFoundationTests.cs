using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ERP.API.Security;
using ERP.Application.Common.Security;
using ERP.Domain.Enums;
using ERP.Infrastructure.Security;
using ERP.Shared.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Backend_ERP.Tests
{
    public class ErpRbacFoundationTests
    {
        private static IHttpContextAccessor CreateHttpContextAccessorWithClaims(IEnumerable<Claim>? claims, bool isAuthenticated = true)
        {
            var context = new DefaultHttpContext();
            if (claims != null && isAuthenticated)
            {
                var identity = new ClaimsIdentity(claims, "Bearer");
                context.User = new ClaimsPrincipal(identity);
            }
            else
            {
                context.User = new ClaimsPrincipal(new ClaimsIdentity());
            }

            var accessor = new HttpContextAccessor { HttpContext = context };
            return accessor;
        }

        private static string GenerateJwtToken(int userId, string role, string email, string name, TimeSpan? lifetime = null)
        {
            var keyBytes = Encoding.UTF8.GetBytes(JwtAuthenticationExtensions.DefaultJwtKey);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("userId", userId.ToString()),
                    new Claim("sub", userId.ToString()),
                    new Claim(ClaimTypes.Role, role),
                    new Claim("role", role),
                    new Claim("email", email),
                    new Claim("name", name)
                }),
                Expires = DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromHours(1)),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(keyBytes),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // ─── Test Group 1: JWT & Claim Extraction ──────────────────────────────

        [Fact]
        public void CurrentUser_extracts_all_claims_from_authenticated_admin()
        {
            var claims = new[]
            {
                new Claim("userId", "101"),
                new Claim("sub", "101"),
                new Claim("role", "Admin"),
                new Claim("email", "admin@company.com"),
                new Claim("name", "Chief Executive Admin")
            };

            var accessor = CreateHttpContextAccessorWithClaims(claims);
            var currentUser = new CurrentUser(accessor);

            Assert.True(currentUser.IsAuthenticated);
            Assert.Equal(101, currentUser.UserId);
            Assert.Equal("admin@company.com", currentUser.Email);
            Assert.Equal("Chief Executive Admin", currentUser.FullName);
            Assert.Equal("Admin", currentUser.Role);
            Assert.True(currentUser.IsAdmin);
            Assert.Equal(AccessScope.All, currentUser.Scope);
        }

        [Fact]
        public void CurrentUser_identifies_unauthenticated_request_correctly()
        {
            var accessor = CreateHttpContextAccessorWithClaims(null, isAuthenticated: false);
            var currentUser = new CurrentUser(accessor);

            Assert.False(currentUser.IsAuthenticated);
            Assert.Null(currentUser.UserId);
            Assert.Null(currentUser.Email);
            Assert.Null(currentUser.FullName);
            Assert.Null(currentUser.Role);
            Assert.False(currentUser.IsAdmin);
            Assert.Equal(AccessScope.Own, currentUser.Scope);
            Assert.False(currentUser.HasPermission("quotations.view"));
        }

        [Fact]
        public void Valid_CRM_JWT_token_can_be_parsed_and_validated()
        {
            var jwt = GenerateJwtToken(202, "Admin", "erpadmin@company.com", "ERP Administrator");
            var handler = new JwtSecurityTokenHandler();
            var keyBytes = Encoding.UTF8.GetBytes(JwtAuthenticationExtensions.DefaultJwtKey);

            var principal = handler.ValidateToken(jwt, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true
            }, out var validatedToken);

            Assert.NotNull(validatedToken);
            Assert.Equal("202", principal.FindFirst("userId")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            Assert.Equal("Admin", principal.FindFirst("role")?.Value ?? principal.FindFirst(ClaimTypes.Role)?.Value);
            Assert.Equal("erpadmin@company.com", principal.FindFirst("email")?.Value ?? principal.FindFirst(ClaimTypes.Email)?.Value);
        }

        // ─── Test Group 2: Admin Scope & Permissions ───────────────────────────

        [Fact]
        public void Admin_role_is_assigned_ALL_scope()
        {
            var claims = new[]
            {
                new Claim("userId", "5"),
                new Claim("role", "Administrator")
            };

            var accessor = CreateHttpContextAccessorWithClaims(claims);
            var currentUser = new CurrentUser(accessor);

            Assert.True(currentUser.IsAdmin);
            Assert.Equal(AccessScope.All, currentUser.Scope);
        }

        [Fact]
        public void Admin_role_has_all_ERP_catalog_permissions()
        {
            var claims = new[]
            {
                new Claim("userId", "1"),
                new Claim("role", "Admin")
            };

            var accessor = CreateHttpContextAccessorWithClaims(claims);
            var currentUser = new CurrentUser(accessor);

            // Test representative permissions across all modules
            Assert.True(currentUser.HasPermission(ErpPermissions.PriceLists.View));
            Assert.True(currentUser.HasPermission(ErpPermissions.PriceLists.Create));
            Assert.True(currentUser.HasPermission(ErpPermissions.PriceLists.Activate));

            Assert.True(currentUser.HasPermission(ErpPermissions.Quotations.View));
            Assert.True(currentUser.HasPermission(ErpPermissions.Quotations.Approve));
            Assert.True(currentUser.HasPermission(ErpPermissions.Quotations.Convert));

            Assert.True(currentUser.HasPermission(ErpPermissions.DiscountApprovals.Approve));
            Assert.True(currentUser.HasPermission(ErpPermissions.DiscountApprovals.Reject));

            Assert.True(currentUser.HasPermission(ErpPermissions.SalesOrders.Create));
            Assert.True(currentUser.HasPermission(ErpPermissions.SalesOrders.Confirm));
            Assert.True(currentUser.HasPermission(ErpPermissions.SalesOrders.GeneratePdf));

            Assert.True(currentUser.HasPermission(ErpPermissions.ProformaInvoices.Approve));
            Assert.True(currentUser.HasPermission(ErpPermissions.ProformaInvoices.Convert));

            Assert.True(currentUser.HasPermission(ErpPermissions.AdvancePayments.Verify));
            Assert.True(currentUser.HasPermission(ErpPermissions.AdvancePayments.Apply));

            Assert.True(currentUser.HasPermission(ErpPermissions.SalesTargets.DashboardView));
            Assert.True(currentUser.HasPermission(ErpPermissions.Performance.LeaderboardView));
            Assert.True(currentUser.HasPermission(ErpPermissions.DispatchLogistics.Lr));
            Assert.True(currentUser.HasPermission(ErpPermissions.Accounting.Outstanding));

            Assert.True(currentUser.HasAnyPermission(ErpPermissions.Quotations.Approve, "random.permission"));
            Assert.Equal(ErpPermissions.All.Count, currentUser.Permissions.Count);
        }

        // ─── Test Group 3: Authorization Service & Scope Rules ─────────────────

        [Fact]
        public void ErpAuthorizationService_allows_admin_to_access_any_record()
        {
            var claims = new[]
            {
                new Claim("userId", "1"),
                new Claim("role", "Admin")
            };

            var accessor = CreateHttpContextAccessorWithClaims(claims);
            var currentUser = new CurrentUser(accessor);
            var authService = new ErpAuthorizationService(currentUser);

            // Record owned by user 999
            Assert.True(authService.CanAccessRecord(999, AccessScope.Own));
            Assert.True(authService.CanAccessRecord((int?)null, AccessScope.Own));
            Assert.True(authService.Authorize(ErpPermissions.Quotations.Approve));
        }

        [Fact]
        public void ErpAuthorizationService_restricts_regular_user_to_own_records()
        {
            var claims = new[]
            {
                new Claim("userId", "42"),
                new Claim("role", "Sales Executive"),
                new Claim("permission", ErpPermissions.Quotations.View)
            };

            var accessor = CreateHttpContextAccessorWithClaims(claims);
            var currentUser = new CurrentUser(accessor);
            var authService = new ErpAuthorizationService(currentUser);

            Assert.False(currentUser.IsAdmin);
            Assert.Equal(AccessScope.Own, currentUser.Scope);

            // Access own record -> Allowed
            Assert.True(authService.CanAccessRecord(42, AccessScope.Own));

            // Access other's record -> Denied
            Assert.False(authService.CanAccessRecord(999, AccessScope.Own));
        }

        // ─── Test Group 4: Permission Catalog Completeness ─────────────────────

        [Fact]
        public void ErpPermissions_catalog_contains_all_required_sales_modules()
        {
            Assert.NotEmpty(ErpPermissions.All);

            // Quotations
            Assert.Contains(ErpPermissions.Quotations.View, ErpPermissions.All);
            Assert.Contains(ErpPermissions.Quotations.Create, ErpPermissions.All);
            Assert.Contains(ErpPermissions.Quotations.Edit, ErpPermissions.All);
            Assert.Contains(ErpPermissions.Quotations.Delete, ErpPermissions.All);
            Assert.Contains(ErpPermissions.Quotations.Duplicate, ErpPermissions.All);
            Assert.Contains(ErpPermissions.Quotations.Submit, ErpPermissions.All);
            Assert.Contains(ErpPermissions.Quotations.Approve, ErpPermissions.All);
            Assert.Contains(ErpPermissions.Quotations.Reject, ErpPermissions.All);
            Assert.Contains(ErpPermissions.Quotations.Return, ErpPermissions.All);
            Assert.Contains(ErpPermissions.Quotations.Reopen, ErpPermissions.All);
            Assert.Contains(ErpPermissions.Quotations.Convert, ErpPermissions.All);
            Assert.Contains(ErpPermissions.Quotations.GeneratePdf, ErpPermissions.All);
            Assert.Contains(ErpPermissions.Quotations.SendEmail, ErpPermissions.All);

            // Sales Orders
            Assert.Contains(ErpPermissions.SalesOrders.View, ErpPermissions.All);
            Assert.Contains(ErpPermissions.SalesOrders.Create, ErpPermissions.All);
            Assert.Contains(ErpPermissions.SalesOrders.Edit, ErpPermissions.All);
            Assert.Contains(ErpPermissions.SalesOrders.Submit, ErpPermissions.All);
            Assert.Contains(ErpPermissions.SalesOrders.Confirm, ErpPermissions.All);
            Assert.Contains(ErpPermissions.SalesOrders.Process, ErpPermissions.All);
            Assert.Contains(ErpPermissions.SalesOrders.Complete, ErpPermissions.All);
            Assert.Contains(ErpPermissions.SalesOrders.Cancel, ErpPermissions.All);
            Assert.Contains(ErpPermissions.SalesOrders.GeneratePdf, ErpPermissions.All);
            Assert.Contains(ErpPermissions.SalesOrders.SendEmail, ErpPermissions.All);
            Assert.Contains(ErpPermissions.SalesOrders.AuditView, ErpPermissions.All);

            // Proforma Invoices
            Assert.Contains(ErpPermissions.ProformaInvoices.View, ErpPermissions.All);
            Assert.Contains(ErpPermissions.ProformaInvoices.Create, ErpPermissions.All);
            Assert.Contains(ErpPermissions.ProformaInvoices.Edit, ErpPermissions.All);
            Assert.Contains(ErpPermissions.ProformaInvoices.Submit, ErpPermissions.All);
            Assert.Contains(ErpPermissions.ProformaInvoices.Approve, ErpPermissions.All);
            Assert.Contains(ErpPermissions.ProformaInvoices.Reject, ErpPermissions.All);
            Assert.Contains(ErpPermissions.ProformaInvoices.Return, ErpPermissions.All);
            Assert.Contains(ErpPermissions.ProformaInvoices.Convert, ErpPermissions.All);
            Assert.Contains(ErpPermissions.ProformaInvoices.Email, ErpPermissions.All);
            Assert.Contains(ErpPermissions.ProformaInvoices.Whatsapp, ErpPermissions.All);
            Assert.Contains(ErpPermissions.ProformaInvoices.GeneratePdf, ErpPermissions.All);

            // Advance Payments
            Assert.Contains(ErpPermissions.AdvancePayments.View, ErpPermissions.All);
            Assert.Contains(ErpPermissions.AdvancePayments.Create, ErpPermissions.All);
            Assert.Contains(ErpPermissions.AdvancePayments.Edit, ErpPermissions.All);
            Assert.Contains(ErpPermissions.AdvancePayments.Verify, ErpPermissions.All);
            Assert.Contains(ErpPermissions.AdvancePayments.Receive, ErpPermissions.All);
            Assert.Contains(ErpPermissions.AdvancePayments.Apply, ErpPermissions.All);

            // Price Lists
            Assert.Contains(ErpPermissions.PriceLists.View, ErpPermissions.All);
            Assert.Contains(ErpPermissions.PriceLists.Create, ErpPermissions.All);
            Assert.Contains(ErpPermissions.PriceLists.Edit, ErpPermissions.All);
            Assert.Contains(ErpPermissions.PriceLists.Delete, ErpPermissions.All);
            Assert.Contains(ErpPermissions.PriceLists.Activate, ErpPermissions.All);
            Assert.Contains(ErpPermissions.PriceLists.Export, ErpPermissions.All);
            Assert.Contains(ErpPermissions.PriceLists.Compare, ErpPermissions.All);

            // Discount Approvals
            Assert.Contains(ErpPermissions.DiscountApprovals.View, ErpPermissions.All);
            Assert.Contains(ErpPermissions.DiscountApprovals.Create, ErpPermissions.All);
            Assert.Contains(ErpPermissions.DiscountApprovals.Approve, ErpPermissions.All);
            Assert.Contains(ErpPermissions.DiscountApprovals.Reject, ErpPermissions.All);
            Assert.Contains(ErpPermissions.DiscountApprovals.Return, ErpPermissions.All);
            Assert.Contains(ErpPermissions.DiscountApprovals.Cancel, ErpPermissions.All);
            Assert.Contains(ErpPermissions.DiscountApprovals.Resubmit, ErpPermissions.All);

            // Sales Targets
            Assert.Contains(ErpPermissions.SalesTargets.View, ErpPermissions.All);
            Assert.Contains(ErpPermissions.SalesTargets.Create, ErpPermissions.All);
            Assert.Contains(ErpPermissions.SalesTargets.Edit, ErpPermissions.All);
            Assert.Contains(ErpPermissions.SalesTargets.Delete, ErpPermissions.All);
            Assert.Contains(ErpPermissions.SalesTargets.Duplicate, ErpPermissions.All);
            Assert.Contains(ErpPermissions.SalesTargets.DashboardView, ErpPermissions.All);

            // Performance
            Assert.Contains(ErpPermissions.Performance.View, ErpPermissions.All);
            Assert.Contains(ErpPermissions.Performance.LeaderboardView, ErpPermissions.All);
            Assert.Contains(ErpPermissions.Performance.Export, ErpPermissions.All);

            // Dispatch Logistics
            Assert.Contains(ErpPermissions.DispatchLogistics.View, ErpPermissions.All);
            Assert.Contains(ErpPermissions.DispatchLogistics.Assign, ErpPermissions.All);
            Assert.Contains(ErpPermissions.DispatchLogistics.Lr, ErpPermissions.All);
            Assert.Contains(ErpPermissions.DispatchLogistics.Eway, ErpPermissions.All);
            Assert.Contains(ErpPermissions.DispatchLogistics.Pod, ErpPermissions.All);

            // Accounting
            Assert.Contains(ErpPermissions.Accounting.View, ErpPermissions.All);
            Assert.Contains(ErpPermissions.Accounting.Outstanding, ErpPermissions.All);
        }

        // ─── Test Group 5: Authorization Filter (401 / 403 / 200) ───────────────

        [Fact]
        public async Task RequirePermissionFilter_returns_401_when_unauthenticated()
        {
            var services = new ServiceCollection();
            services.AddScoped<ICurrentUser>(_ => new CurrentUser(CreateHttpContextAccessorWithClaims(null, isAuthenticated: false)));
            var serviceProvider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var authContext = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());

            var filter = new RequirePermissionAttribute(ErpPermissions.Quotations.Approve);
            await filter.OnAuthorizationAsync(authContext);

            Assert.NotNull(authContext.Result);
            Assert.IsType<UnauthorizedObjectResult>(authContext.Result);
        }

        [Fact]
        public async Task RequirePermissionFilter_allows_admin_to_pass()
        {
            var claims = new[]
            {
                new Claim("userId", "1"),
                new Claim("role", "Admin")
            };

            var services = new ServiceCollection();
            services.AddScoped<ICurrentUser>(_ => new CurrentUser(CreateHttpContextAccessorWithClaims(claims, isAuthenticated: true)));
            var serviceProvider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var authContext = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());

            var filter = new RequirePermissionAttribute(ErpPermissions.Quotations.Approve);
            await filter.OnAuthorizationAsync(authContext);

            // Result is null -> authorization passed successfully
            Assert.Null(authContext.Result);
        }

        [Fact]
        public async Task RequirePermissionFilter_returns_403_when_user_lacks_permission()
        {
            var claims = new[]
            {
                new Claim("userId", "2"),
                new Claim("role", "Sales Executive"),
                new Claim("permission", ErpPermissions.Quotations.View)
            };

            var services = new ServiceCollection();
            services.AddScoped<ICurrentUser>(_ => new CurrentUser(CreateHttpContextAccessorWithClaims(claims, isAuthenticated: true)));
            var serviceProvider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var authContext = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());

            var filter = new RequirePermissionAttribute(ErpPermissions.Quotations.Approve);
            await filter.OnAuthorizationAsync(authContext);

            Assert.NotNull(authContext.Result);
            var objectResult = Assert.IsType<ObjectResult>(authContext.Result);
            Assert.Equal(403, objectResult.StatusCode);
        }
    }
}
