using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    [Table("Tasks")]
    public class TaskTable : IAuditableByUser
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TaskId { get; set; }

        public string TaskTitle { get; set; } = string.Empty;

        public string TaskDescription { get; set; } = string.Empty;

        public string TaskStatus { get; set; } = string.Empty;

        public string TaskAssignee { get; set; } = string.Empty;

        public DateTime TaskDueDate { get; set; } = DateTime.UtcNow;

        public string TaskPriority { get; set; } = string.Empty;

        public int? AssigneeUserId { get; set; }

        public int? RelatedLeadId { get; set; }

        public int? RelatedDealId { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        public DateTime LastModified { get; set; }

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
