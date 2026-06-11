using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    [Table("lead_sync_interval_options")]
    public class LeadSyncIntervalOption
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("hours")]
        public int Hours { get; set; }

        [Column("label")]
        [MaxLength(64)]
        public string Label { get; set; } = string.Empty;

        [Column("sort_order")]
        public int SortOrder { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
