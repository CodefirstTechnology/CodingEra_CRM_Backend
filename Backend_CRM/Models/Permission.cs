using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    /// <summary>Granular CRM permission (module + action). Codes are stored in DB — not hard-coded at runtime.</summary>
    [Table("crm_permissions")]
    public class Permission
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("module")]
        [MaxLength(64)]
        public string Module { get; set; } = string.Empty;

        [Column("action")]
        [MaxLength(64)]
        public string Action { get; set; } = string.Empty;

        [Column("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>Unique permission code, e.g. <c>leads.view</c>.</summary>
        [Column("code")]
        [MaxLength(128)]
        public string Code { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
