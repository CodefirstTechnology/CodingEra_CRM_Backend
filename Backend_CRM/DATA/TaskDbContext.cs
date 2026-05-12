using CRM.models;
using Microsoft.EntityFrameworkCore;
namespace CRM.DATA
{
    public class TaskDbcontext : DbContext
    {
        public TaskDbcontext(DbContextOptions<TaskDbcontext> options) : base(options)
        {
        }

        public DbSet<TaskTable> Tasks { get; set; }

        public DbSet<CallLog> CallLogs { get; set; }

        public DbSet<Note> Notes { get; set; }
    }
}
