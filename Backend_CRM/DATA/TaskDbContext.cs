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

        public DbSet<User> Users { get; set; }

        public DbSet<Organization> Organizations { get; set; }

        public DbSet<Contact> Contacts { get; set; }

        public DbSet<Lead> Leads { get; set; }

        public DbSet<Deal> Deals { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Lead>()
                .HasIndex(l => l.ExternalRef)
                .IsUnique()
                .HasFilter("\"external_ref\" IS NOT NULL AND \"external_ref\" <> ''");

            // --- Foreign keys: optional links use SET NULL on parent delete (safe for CRM workflows) ---

            modelBuilder.Entity<Contact>()
                .HasOne<Organization>()
                .WithMany()
                .HasForeignKey(c => c.OrganizationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Lead>()
                .HasOne<Organization>()
                .WithMany()
                .HasForeignKey(l => l.OrganizationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Lead>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(l => l.LeadOwnerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Deal>()
                .HasOne<Organization>()
                .WithMany()
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Deal>()
                .HasOne<Organization>()
                .WithMany()
                .HasForeignKey(d => d.RelatedOrganizationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Deal>()
                .HasOne<Contact>()
                .WithMany()
                .HasForeignKey(d => d.ContactId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Deal>()
                .HasOne<Contact>()
                .WithMany()
                .HasForeignKey(d => d.RelatedContactId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Deal>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(d => d.DealOwnerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Deal>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(d => d.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Note>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(n => n.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Note>()
                .HasOne<Lead>()
                .WithMany()
                .HasForeignKey(n => n.RelatedLeadId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Note>()
                .HasOne<Deal>()
                .WithMany()
                .HasForeignKey(n => n.RelatedDealId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Note>()
                .HasOne<Contact>()
                .WithMany()
                .HasForeignKey(n => n.RelatedContactId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Note>()
                .HasOne<Organization>()
                .WithMany()
                .HasForeignKey(n => n.RelatedOrganizationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TaskTable>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.AssigneeUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TaskTable>()
                .HasOne<Lead>()
                .WithMany()
                .HasForeignKey(t => t.RelatedLeadId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TaskTable>()
                .HasOne<Deal>()
                .WithMany()
                .HasForeignKey(t => t.RelatedDealId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CallLog>()
                .HasOne<Contact>()
                .WithMany()
                .HasForeignKey(c => c.ContactId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CallLog>()
                .HasOne<Lead>()
                .WithMany()
                .HasForeignKey(c => c.RelatedLeadId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CallLog>()
                .HasOne<Deal>()
                .WithMany()
                .HasForeignKey(c => c.RelatedDealId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
