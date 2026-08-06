using HRMS.Models;

namespace HRMS.Authorization;

public static class RoleMapper
{
    public static UserRole ToUserRole(Role role) =>
        Enum.Parse<UserRole>(role.Code, ignoreCase: true);

    public static UserRole ToUserRole(string? roleCode) =>
        Enum.TryParse<UserRole>(roleCode, ignoreCase: true, out var role)
            ? role
            : UserRole.EMPLOYEE;

    public static string ToRoleCode(UserRole role) => role.ToString();
}
