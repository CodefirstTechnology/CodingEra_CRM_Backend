namespace CRM.DTO
{
    public class RegisterRequest
    {
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        /// <summary>FK to <c>crm_roles</c>. Omit or <c>0</c> to assign the active role named <c>user</c> (case-insensitive).</summary>
        public int? RoleId { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }

    public class UserSessionDto
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public int? RoleId { get; set; }

        /// <summary>Role display name from <c>crm_roles</c>.</summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>Opaque session token (replace with JWT when auth middleware is added).</summary>
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>User row for admin/UI lists — no credentials or session token.</summary>
    public class UserListItemDto
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public int? RoleId { get; set; }

        public string Role { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
