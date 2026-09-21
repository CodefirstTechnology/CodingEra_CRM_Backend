namespace HRMS.Authorization;

public static class HrmsPermissions
{
    // SaaS Platform & Super Admin
    public const string TenantsView = "tenants.view";
    public const string TenantsManage = "tenants.manage";
    public const string TenantAdminsManage = "tenants.manage_admins";
    public const string PlatformAuditView = "platform.audit_view";
    public const string PlatformReportsView = "platform.reports_view";
    public const string SystemSettings = "system.settings";
    public const string HealthView = "health.view";

    // User Administration & Org
    public const string ManageHrAdmins = "users.manage_hr_admins";
    public const string ManageEmployees = "users.manage_employees";
    public const string OrganizationManage = "organization.manage";

    // Employee Master
    public const string EmployeesViewAll = "employees.view_all";
    public const string EmployeesViewOwn = "employees.view_own";
    public const string EmployeesCreate = "employees.create";
    public const string EmployeesUpdate = "employees.update";
    public const string EmployeesUpdateOwn = "employees.update_own";
    public const string EmployeesDelete = "employees.delete";

    // Attendance
    public const string AttendanceViewAll = "attendance.view_all";
    public const string AttendanceViewOwn = "attendance.view_own";
    public const string AttendanceManage = "attendance.manage";
    public const string AttendanceMarkOwn = "attendance.mark_own";

    // Leave Management
    public const string LeaveViewAll = "leave.view_all";
    public const string LeaveViewOwn = "leave.view_own";
    public const string LeaveApply = "leave.apply";
    public const string LeaveApprove = "leave.approve";
    public const string LeaveManage = "leave.manage";

    // Payroll & Payslips
    public const string PayrollProcess = "payroll.process";
    public const string PayslipsViewAll = "payslips.view_all";
    public const string PayslipsViewOwn = "payslips.view_own";

    // Employee Documents & Vault
    public const string DocumentsManageAll = "documents.manage_all";
    public const string DocumentsManageOwn = "documents.manage_own";
    public const string DocumentsView = "documents.view";
    public const string DocumentsCreate = "documents.create";
    public const string DocumentsUpdate = "documents.update";
    public const string DocumentsUpload = "documents.upload";
    public const string DocumentsVerify = "documents.verify";
    public const string DocumentsReject = "documents.reject";
    public const string DocumentsDownload = "documents.download";
    public const string DocumentsConfigure = "documents.configure";
    public const string DocumentsExpiry = "documents.expiry";
    public const string DocumentsReport = "documents.report";
    public const string DocumentsAudit = "documents.audit";
    public const string DocumentsSelfView = "documents.self.view";
    public const string DocumentsSelfUpload = "documents.self.upload";
    public const string DocumentsSelfDownload = "documents.self.download";
    public const string DocumentsSelfReplace = "documents.self.replace";

    // Performance Reviews
    public const string PerformanceManageAll = "performance.manage_all";
    public const string PerformanceSelfReview = "performance.self_review";

    // Assets Management
    public const string AssetsManage = "assets.manage";
    public const string AssetsViewAll = "assets.view_all";
    public const string AssetsViewOwn = "assets.view_own";

    // Reports, Audit & Master Data
    public const string ReportsView = "reports.view";
    public const string MasterDataManage = "master_data.manage";
    public const string AuditViewTenant = "audit.view_tenant";
}
