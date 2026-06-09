using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CRM.models
{
    [Table("user_targets")]
    public class UserTarget : IAuditableByUser
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public User? User { get; set; }

        [Column("target_type_id")]
        public int TargetTypeId { get; set; }

        [ForeignKey(nameof(TargetTypeId))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public UserTargetType? TargetType { get; set; }

        [Column("target_amount")]
        public decimal TargetAmount { get; set; }

        [Column("start_date")]
        public DateOnly StartDate { get; set; }

        [Column("end_date")]
        public DateOnly EndDate { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>Cached sum of qualifying deal amounts; recalculated on deal or target changes.</summary>
        [Column("achieved_amount")]
        public decimal AchievedAmount { get; set; }

        [Column("achieved_calculated_at")]
        public DateTime? AchievedCalculatedAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("last_modified")]
        public DateTime LastModified { get; set; }

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [Column("updated_by")]
        public int? UpdatedBy { get; set; }
    }
}
