using HRMS.Models;

namespace HRMS.Authorization;

public static class HrmsRolePermissions
{
    private static readonly IReadOnlyDictionary<UserRole, HashSet<string>> RoleMap =
        new Dictionary<UserRole, HashSet<string>>
        {
            [UserRole.SUPER_ADMIN] = new(StringComparer.OrdinalIgnoreCase)
            {
                HrmsPermissions.TenantsView,
                HrmsPermissions.TenantsManage,
                HrmsPermissions.TenantAdminsManage,
                HrmsPermissions.PlatformAuditView,
                HrmsPermissions.PlatformReportsView,
                HrmsPermissions.SystemSettings,
                HrmsPermissions.HealthView,
                HrmsPermissions.ManageHrAdmins,
                HrmsPermissions.OrganizationManage
            },
            [UserRole.HR_ADMIN] = new(StringComparer.OrdinalIgnoreCase)
            {
                HrmsPermissions.OrganizationManage,
                HrmsPermissions.ManageEmployees,
                HrmsPermissions.EmployeesViewAll,
                HrmsPermissions.EmployeesViewOwn,
                HrmsPermissions.EmployeesCreate,
                HrmsPermissions.EmployeesUpdate,
                HrmsPermissions.EmployeesUpdateOwn,
                HrmsPermissions.EmployeesDelete,
                HrmsPermissions.AttendanceViewAll,
                HrmsPermissions.AttendanceViewOwn,
                HrmsPermissions.AttendanceManage,
                HrmsPermissions.AttendanceMarkOwn,
                HrmsPermissions.LeaveViewAll,
                HrmsPermissions.LeaveViewOwn,
                HrmsPermissions.LeaveApply,
                HrmsPermissions.LeaveApprove,
                HrmsPermissions.LeaveManage,
                HrmsPermissions.PayrollProcess,
                HrmsPermissions.PayslipsViewAll,
                HrmsPermissions.PayslipsViewOwn,
                HrmsPermissions.DocumentsManageAll,
                HrmsPermissions.DocumentsManageOwn,
                HrmsPermissions.PerformanceManageAll,
                HrmsPermissions.PerformanceSelfReview,
                HrmsPermissions.AssetsManage,
                HrmsPermissions.AssetsViewAll,
                HrmsPermissions.AssetsViewOwn,
                HrmsPermissions.ReportsView,
                HrmsPermissions.MasterDataManage,
                HrmsPermissions.AuditViewTenant
            },
            [UserRole.EMPLOYEE] = new(StringComparer.OrdinalIgnoreCase)
            {
                HrmsPermissions.EmployeesViewOwn,
                HrmsPermissions.EmployeesUpdateOwn,
                HrmsPermissions.AttendanceViewOwn,
                HrmsPermissions.AttendanceMarkOwn,
                HrmsPermissions.LeaveViewOwn,
                HrmsPermissions.LeaveApply,
                HrmsPermissions.PayslipsViewOwn,
                HrmsPermissions.DocumentsManageOwn,
                HrmsPermissions.PerformanceSelfReview,
                HrmsPermissions.AssetsViewOwn
            }
        };

    public static IReadOnlyCollection<string> GetPermissions(UserRole role) =>
        RoleMap.TryGetValue(role, out var permissions)
            ? permissions
            : Array.Empty<string>();

    public static bool HasPermission(UserRole role, string permission) =>
        RoleMap.TryGetValue(role, out var permissions) && permissions.Contains(permission);

    public static bool IsAdminRole(UserRole role) =>
        role is UserRole.SUPER_ADMIN or UserRole.HR_ADMIN;
}
