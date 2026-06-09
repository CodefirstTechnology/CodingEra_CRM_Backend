using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CRM.models
{
    [Table("lead_sync_source_configs")]
    public class LeadSyncSourceConfig
    {
        [Key]
        [Column("source_id")]
        public int SourceId { get; set; }

        [ForeignKey(nameof(SourceId))]
        [JsonIgnore]
        public LeadSyncSource? Source { get; set; }

        [Column("auto_sync_enabled")]
        public bool AutoSyncEnabled { get; set; }

        [Column("interval_option_id")]
        public int? IntervalOptionId { get; set; }

        [ForeignKey(nameof(IntervalOptionId))]
        [JsonIgnore]
        public LeadSyncIntervalOption? IntervalOption { get; set; }

        [Column("last_sync_at")]
        public DateTime? LastSyncAt { get; set; }

        [Column("next_sync_at")]
        public DateTime? NextSyncAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("updated_by")]
        public int? UpdatedBy { get; set; }

        [ForeignKey(nameof(UpdatedBy))]
        [JsonIgnore]
        public User? UpdatedByUser { get; set; }
    }
}
