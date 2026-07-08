using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CRM.models
{
    [Table("lead_sync_round_robin_states")]
    public class LeadSyncRoundRobinState
    {
        [Key]
        [Column("source_id")]
        public int SourceId { get; set; }

        [ForeignKey(nameof(SourceId))]
        [JsonIgnore]
        public LeadSyncSource? Source { get; set; }

        [Column("next_index")]
        public int NextIndex { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
