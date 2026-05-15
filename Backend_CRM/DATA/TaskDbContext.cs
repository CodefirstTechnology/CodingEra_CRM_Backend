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

        public DbSet<LeadHistory> LeadHistories { get; set; }

        public DbSet<Deal> Deals { get; set; }

        public DbSet<Salutation> Salutations { get; set; }

        public DbSet<EmployeeCount> EmployeeCounts { get; set; }

        public DbSet<Territory> Territories { get; set; }

        public DbSet<Industry> Industries { get; set; }

        public DbSet<LeadStatus> LeadStatuses { get; set; }

        public DbSet<RequestType> RequestTypes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Salutation>()
                .HasIndex(s => s.Name)
                .IsUnique();

            modelBuilder.Entity<EmployeeCount>()
                .HasIndex(e => e.Name)
                .IsUnique();

            modelBuilder.Entity<Territory>()
                .HasIndex(t => t.Name)
                .IsUnique();

            modelBuilder.Entity<Industry>()
                .HasIndex(i => i.Name)
                .IsUnique();

            modelBuilder.Entity<LeadStatus>()
                .HasIndex(s => s.Name)
                .IsUnique();

            modelBuilder.Entity<RequestType>()
                .HasIndex(r => r.Name)
                .IsUnique();

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

            modelBuilder.Entity<Lead>()
                .HasOne(l => l.Salutation)
                .WithMany()
                .HasForeignKey(l => l.SalutationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Lead>()
                .HasOne(l => l.LeadStatus)
                .WithMany()
                .HasForeignKey(l => l.LeadStatusId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Lead>()
                .HasOne(l => l.RequestType)
                .WithMany()
                .HasForeignKey(l => l.RequestTypeId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<LeadHistory>()
                .HasOne<Lead>()
                .WithMany()
                .HasForeignKey(h => h.LeadId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LeadHistory>()
                .HasIndex(h => h.LeadId);

            modelBuilder.Entity<Organization>()
                .HasOne(o => o.Industry)
                .WithMany()
                .HasForeignKey(o => o.IndustryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Organization>()
                .HasOne(o => o.EmployeeCount)
                .WithMany()
                .HasForeignKey(o => o.EmployeeCountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Organization>()
                .HasOne(o => o.Territory)
                .WithMany()
                .HasForeignKey(o => o.TerritoryId)
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
            modelBuilder.Entity<EmployeeCount>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<Territory>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<Industry>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<LeadStatus>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<RequestType>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<User>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<Organization>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<Contact>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<Lead>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<LeadHistory>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<Deal>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<Note>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<TaskTable>().Property(e => e.TaskId).UseIdentityAlwaysColumn();
            modelBuilder.Entity<CallLog>().Property(e => e.CallId).UseIdentityAlwaysColumn();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            StampAuditTimestamps();
            AppendLeadHistorySnapshots();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            StampAuditTimestamps();
            AppendLeadHistorySnapshots();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        /// <summary>Stores the previous scalar state of each modified <see cref="Lead"/> before updates are persisted.</summary>
        private void AppendLeadHistorySnapshots()
        {
            var utc = DateTime.UtcNow;
            foreach (var entry in ChangeTracker.Entries<Lead>())
            {
                if (entry.State != EntityState.Modified)
                {
                    continue;
                }

                var o = entry.OriginalValues;
                LeadHistories.Add(new LeadHistory
                {
                    LeadId = entry.Entity.Id,
                    ArchivedAt = utc,
                    FirstName = (string)o[nameof(Lead.FirstName)]!,
                    LastName = (string)o[nameof(Lead.LastName)]!,
                    SalutationId = (int?)o[nameof(Lead.SalutationId)],
                    Gender = (string)o[nameof(Lead.Gender)]!,
                    Mobile = (string)o[nameof(Lead.Mobile)]!,
                    Email = (string)o[nameof(Lead.Email)]!,
                    OrganizationId = (int?)o[nameof(Lead.OrganizationId)],
                    LeadStatusId = (int?)o[nameof(Lead.LeadStatusId)],
                    RequestTypeId = (int?)o[nameof(Lead.RequestTypeId)],
                    Notes = (string)o[nameof(Lead.Notes)]!,
                    LeadOwnerName = (string)o[nameof(Lead.LeadOwnerName)]!,
                    Owner = (string)o[nameof(Lead.Owner)]!,
                    LeadOwnerId = (int?)o[nameof(Lead.LeadOwnerId)],
                    LeadSource = (string)o[nameof(Lead.LeadSource)]!,
                    CreatedAt = (DateTime?)o[nameof(Lead.CreatedAt)],
                    UpdatedAt = (DateTime)o[nameof(Lead.UpdatedAt)]!,
                });
            }
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
                            case IMasterDataEntity master:
                                master.LastModified = utc;
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
                            case IMasterDataEntity master:
                                master.LastModified = utc;
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
