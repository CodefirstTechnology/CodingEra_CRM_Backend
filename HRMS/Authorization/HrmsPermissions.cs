namespace HRMS.Authorization;

public static class HrmsPermissions
{
    public const string SystemSettings = "system.settings";
    public const string ManageHrAdmins = "users.manage_hr_admins";
    public const string ManageEmployees = "users.manage_employees";

    public const string EmployeesViewAll = "employees.view_all";
    public const string EmployeesViewOwn = "employees.view_own";
    public const string EmployeesCreate = "employees.create";
    public const string EmployeesUpdate = "employees.update";
    public const string EmployeesUpdateOwn = "employees.update_own";
    public const string EmployeesDelete = "employees.delete";

    public const string AttendanceViewAll = "attendance.view_all";
    public const string AttendanceViewOwn = "attendance.view_own";
    public const string AttendanceManage = "attendance.manage";
    public const string AttendanceMarkOwn = "attendance.mark_own";

    public const string LeaveViewAll = "leave.view_all";
    public const string LeaveViewOwn = "leave.view_own";
    public const string LeaveApply = "leave.apply";
    public const string LeaveApprove = "leave.approve";
    public const string LeaveManage = "leave.manage";

    public const string PayrollProcess = "payroll.process";
    public const string PayslipsViewAll = "payslips.view_all";
    public const string PayslipsViewOwn = "payslips.view_own";

    public const string DocumentsManageAll = "documents.manage_all";
    public const string DocumentsManageOwn = "documents.manage_own";

    public const string PerformanceManageAll = "performance.manage_all";
    public const string PerformanceSelfReview = "performance.self_review";

    public const string ReportsView = "reports.view";
    public const string MasterDataManage = "master_data.manage";
    public const string HealthView = "health.view";
}
