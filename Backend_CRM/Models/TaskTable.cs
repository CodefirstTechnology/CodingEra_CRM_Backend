using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.models
{
    [Table("Tasks")]
    public class TaskTable
    {

        [Key]
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

        public DateTime LastModified { get; set; }
    }
}
