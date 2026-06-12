using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    [Table("user_dashboard_preferences")]
    public class UserDashboardPreference
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("morning_briefing_enabled")]
        public bool MorningBriefingEnabled { get; set; } = true;

        [Column("last_briefing_played_date")]
        public DateOnly? LastBriefingPlayedDate { get; set; }

        [Column("cached_briefing_date")]
        public DateOnly? CachedBriefingDate { get; set; }

        [Column("cached_briefing_message")]
        [MaxLength(1000)]
        public string? CachedBriefingMessage { get; set; }

        [Column("cached_briefing_source")]
        [MaxLength(32)]
        public string? CachedBriefingSource { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
