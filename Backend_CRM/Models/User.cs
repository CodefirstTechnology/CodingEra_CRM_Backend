using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    [Table("users")]
    public class User
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

        [Column("role")]
        [MaxLength(64)]
        public string Role { get; set; } = "user";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
