using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CRM.models
{
    [Table("users")]
    public class User : IAuditableByUser
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("full_name")]
        [MaxLength(256)]
        public string FullName { get; set; } = string.Empty;

        [Column("email")]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Column("phone")]
        [MaxLength(64)]
        public string Phone { get; set; } = string.Empty;

        [Column("password_hash")]
        [MaxLength(256)]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("role_id")]
        public int? RoleId { get; set; }

        [ForeignKey(nameof(RoleId))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Role? Role { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [Column("updated_by")]
        public int? UpdatedBy { get; set; }
    }
}
