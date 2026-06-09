using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CRM.models
{
    [Table("lead_sync_source_assignments")]
    public class LeadSyncSourceAssignment
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("source_id")]
        public int SourceId { get; set; }

        [ForeignKey(nameof(SourceId))]
        [JsonIgnore]
        public LeadSyncSource? Source { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        [JsonIgnore]
        public User? User { get; set; }

        [Column("sort_order")]
        public int SortOrder { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        [JsonIgnore]
        public User? CreatedByUser { get; set; }
    }
}
