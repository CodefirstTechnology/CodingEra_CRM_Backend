using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CRM.models
{
    [Table("lead_sync_sources")]
    public class LeadSyncSource
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Stable slug used in APIs (e.g. indiamart).</summary>
        [Column("code")]
        [MaxLength(64)]
        public string Code { get; set; } = string.Empty;

        [Column("display_name")]
        [MaxLength(128)]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Matches [crm-ext:{MarkerName}:…] in lead notes.</summary>
        [Column("marker_name")]
        [MaxLength(64)]
        public string MarkerName { get; set; } = string.Empty;

        [Column("sort_order")]
        public int SortOrder { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>When true, manual/auto sync can invoke the provider.</summary>
        [Column("api_integration_ready")]
        public bool ApiIntegrationReady { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonIgnore]
        public LeadSyncSourceConfig? Config { get; set; }

        [JsonIgnore]
        public LeadSyncSourceCredentials? Credentials { get; set; }

        [JsonIgnore]
        public ICollection<LeadSyncSourceAssignment> Assignments { get; set; } = new List<LeadSyncSourceAssignment>();

        [JsonIgnore]
        public LeadSyncRoundRobinState? RoundRobinState { get; set; }
    }
}
