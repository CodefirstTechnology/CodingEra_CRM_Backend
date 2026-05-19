using System.ComponentModel.DataAnnotations;

namespace CRM.DTO
{
    public class TaskDto
    {
        [Key]
        public int TaskId { get; set; }

        public string TaskTitle { get; set; }

        public string TaskDescription { get; set; }

        public string TaskStatus { get; set; }

        public string TaskAssignee { get; set; }

        public DateTime TaskDueDate { get; set; }

        public string TaskPriority { get; set; }
    }
}
