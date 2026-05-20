using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    [Table("comments")]
    public class Comment : IAuditableByUser
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>lead | deal | contact | organization</summary>
        [Column("entity_type")]
        [MaxLength(32)]
        public string EntityType { get; set; } = "lead";

        [Column("entity_id")]
        public int EntityId { get; set; }

        [Column("author_id")]
        public int? AuthorId { get; set; }

        [Column("body")]
        public string Body { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [Column("updated_by")]
        public int? UpdatedBy { get; set; }
    }
}
