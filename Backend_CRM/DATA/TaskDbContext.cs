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

        public DbSet<Salutation> Salutations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Salutation>()
                .HasIndex(s => s.Name)
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
                .HasOne(l => l.Organization)
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

            // Integer PKs: PostgreSQL GENERATED ALWAYS AS IDENTITY (values come from the database only).
            modelBuilder.Entity<Salutation>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<User>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<Organization>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<Contact>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<Lead>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<Deal>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<Note>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<TaskTable>().Property(e => e.TaskId).UseIdentityAlwaysColumn();
            modelBuilder.Entity<CallLog>().Property(e => e.CallId).UseIdentityAlwaysColumn();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            StampAuditTimestamps();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            StampAuditTimestamps();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        /// <summary>Sets server-side audit timestamps so APIs do not need to send them.</summary>
        private void StampAuditTimestamps()
        {
            var utc = DateTime.UtcNow;
            foreach (var entry in ChangeTracker.Entries())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        switch (entry.Entity)
                        {
                            case Salutation s:
                                s.LastModified = utc;
                                break;
                            case Organization o:
                                o.LastModified = utc;
                                break;
                            case Contact c:
                                c.LastModified = utc;
                                break;
                            case Deal d:
                                d.LastModified = utc;
                                break;
                            case TaskTable t:
                                t.LastModified = utc;
                                break;
                            case CallLog cl:
                                cl.LastModified = utc;
                                break;
                            case Note n:
                                n.CreatedAt = utc;
                                n.UpdatedAt = utc;
                                break;
                            case Lead l:
                                l.UpdatedAt = utc;
                                break;
                        }

                        break;
                    case EntityState.Modified:
                        switch (entry.Entity)
                        {
                            case Salutation s:
                                s.LastModified = utc;
                                break;
                            case Organization o:
                                o.LastModified = utc;
                                break;
                            case Contact c:
                                c.LastModified = utc;
                                break;
                            case Deal d:
                                d.LastModified = utc;
                                break;
                            case TaskTable t:
                                t.LastModified = utc;
                                break;
                            case CallLog cl:
                                cl.LastModified = utc;
                                break;
                            case Note n:
                                n.UpdatedAt = utc;
                                break;
                            case Lead l:
                                l.UpdatedAt = utc;
                                break;
                        }

                        break;
                }
            }
        }
    }
}
