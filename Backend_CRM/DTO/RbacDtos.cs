using CRM.models;

namespace CRM.DTO
{
    public class PermissionDto
    {
        public int Id { get; set; }

        public string Module { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }

    public class RolePermissionAssignmentDto
    {
        public int PermissionId { get; set; }

        public string Code { get; set; } = string.Empty;

        public AccessScope AccessScope { get; set; }
    }

    public class RoleListItemDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public int AssignedUserCount { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class RoleDetailDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public int AssignedUserCount { get; set; }

        public IReadOnlyList<RolePermissionAssignmentDto> Permissions { get; set; } = Array.Empty<RolePermissionAssignmentDto>();
    }

    public class RoleUpsertDto
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }

    public class RolePermissionsUpdateDto
    {
        public IReadOnlyList<RolePermissionAssignmentDto> Permissions { get; set; } = Array.Empty<RolePermissionAssignmentDto>();
    }

    public class UserPermissionDto
    {
        public string Code { get; set; } = string.Empty;

        public string Module { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public AccessScope AccessScope { get; set; }
    }

    public class UpdateUserRoleRequest
    {
        public int RoleId { get; set; }
    }

    public class RbacAccessDiagnosticDto
    {
        public int UserId { get; set; }

        public int? RoleId { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public bool IsAdminUser { get; set; }

        public IReadOnlyList<string> PermissionCodes { get; set; } = Array.Empty<string>();
    }
}
