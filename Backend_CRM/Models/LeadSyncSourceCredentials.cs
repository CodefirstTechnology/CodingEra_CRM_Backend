using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CRM.models
{
    [Table("lead_sync_source_credentials")]
    public class LeadSyncSourceCredentials
    {
        [Key]
        [Column("source_id")]
        public int SourceId { get; set; }

        [ForeignKey(nameof(SourceId))]
        [JsonIgnore]
        public LeadSyncSource? Source { get; set; }

        [Column("pull_api_url")]
        [MaxLength(512)]
        public string? PullApiUrl { get; set; }

        [Column("api_key_encrypted")]
        public string? ApiKeyEncrypted { get; set; }

        [Column("configured_at")]
        public DateTime? ConfiguredAt { get; set; }

        [Column("configured_by")]
        public int? ConfiguredBy { get; set; }

        [ForeignKey(nameof(ConfiguredBy))]
        [JsonIgnore]
        public User? ConfiguredByUser { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
