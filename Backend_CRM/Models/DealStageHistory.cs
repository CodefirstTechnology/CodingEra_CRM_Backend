using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    /// <summary>Immutable record of a deal pipeline stage transition.</summary>
    [Table("deal_stage_histories")]
    public class DealStageHistory
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("deal_id")]
        public int DealId { get; set; }

        [Column("previous_stage")]
        [MaxLength(128)]
        public string PreviousStage { get; set; } = string.Empty;

        [Column("new_stage")]
        [MaxLength(128)]
        public string NewStage { get; set; } = string.Empty;

        [Column("changed_by_user_id")]
        public int? ChangedByUserId { get; set; }

        [Column("changed_at")]
        public DateTime ChangedAt { get; set; }

        [Column("comment")]
        public string Comment { get; set; } = string.Empty;
    }
}
