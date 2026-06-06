using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CRM.models
{
    /// <summary>Maps a role to a permission with an access scope (Own / Team / All).</summary>
    [Table("crm_role_permissions")]
    [PrimaryKey(nameof(RoleId), nameof(PermissionId))]
    public class RolePermission
    {
        [Column("role_id")]
        public int RoleId { get; set; }

        [ForeignKey(nameof(RoleId))]
        public Role Role { get; set; } = null!;

        [Column("permission_id")]
        public int PermissionId { get; set; }

        [ForeignKey(nameof(PermissionId))]
        public Permission Permission { get; set; } = null!;

        [Column("access_scope")]
        public AccessScope AccessScope { get; set; } = AccessScope.Own;
    }
}
