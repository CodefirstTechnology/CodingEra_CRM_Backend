using System.ComponentModel.DataAnnotations;
namespace CRM.models
{
    public class TaskTable
    {

        [Key]
        public int TaskId { get; set; }

        public string TaskTitle { get; set; } = string.Empty;

        public string TaskDescription { get; set; } = string.Empty;

        public string TaskStatus { get; set; } = string.Empty;

        public string TaskAssignee { get; set; } = string.Empty;

        public DateTime TaskDueDate { get; set; } = DateTime.Now;

        public string TaskPriority { get; set; } = string.Empty;
    }
}
