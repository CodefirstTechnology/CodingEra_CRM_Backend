using CRM.Helpers;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.DATA
{
    public class TaskDbcontext : DbContext
    {
        public TaskDbcontext(DbContextOptions<TaskDbcontext> options) : base(options)
        {
        }

        /// <summary>Controllers set this before <see cref="SaveChanges()"/> so <see cref="IAuditableByUser"/> rows get created/updated-by stamps.</summary>
        public int? AuditUserId { get; set; }

        /// <summary>Optional comment for the next deal stage change (cleared after save).</summary>
        public string? DealStageChangeComment { get; set; }

        public DbSet<TaskTable> Tasks { get; set; }

        public DbSet<CallLog> CallLogs { get; set; }

        public DbSet<Note> Notes { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Organization> Organizations { get; set; }

        public DbSet<Contact> Contacts { get; set; }

        public DbSet<Lead> Leads { get; set; }

        public DbSet<LeadHistory> LeadHistories { get; set; }

        public DbSet<Deal> Deals { get; set; }

        public DbSet<DealStageHistory> DealStageHistories { get; set; }

        public DbSet<Salutation> Salutations { get; set; }

        public DbSet<EmployeeCount> EmployeeCounts { get; set; }

        public DbSet<Territory> Territories { get; set; }

        public DbSet<Industry> Industries { get; set; }

        public DbSet<LeadStatus> LeadStatuses { get; set; }

        public DbSet<DealStatus> DealStatuses { get; set; }

        public DbSet<RequestType> RequestTypes { get; set; }

        public DbSet<Role> Roles { get; set; }

        public DbSet<Permission> Permissions { get; set; }

        public DbSet<RolePermission> RolePermissions { get; set; }

        public DbSet<ActivityLog> ActivityLogs { get; set; }

        public DbSet<Comment> Comments { get; set; }

        public DbSet<Email> Emails { get; set; }

        public DbSet<Quotation> Quotations { get; set; }

        public DbSet<QuotationLineItem> QuotationLineItems { get; set; }

        public DbSet<QuotationSettings> QuotationSettings { get; set; }
        public DbSet<CompanyProfile> CompanyProfiles { get; set; }

        public DbSet<QuotationFiscalSequence> QuotationFiscalSequences { get; set; }

        public DbSet<QuotationItemGridDefault> QuotationItemGridDefaults { get; set; }

        public DbSet<QuotationItemGridUserPreference> QuotationItemGridUserPreferences { get; set; }

        public DbSet<ItemGroup> ItemGroups { get; set; }

        public DbSet<ItemAttribute> ItemAttributes { get; set; }

        public DbSet<ItemAttributeValue> ItemAttributeValues { get; set; }

        public DbSet<Item> Items { get; set; }

        public DbSet<ItemSpecification> ItemSpecifications { get; set; }

        public DbSet<ItemTemplateAttribute> ItemTemplateAttributes { get; set; }

        public DbSet<ItemVariantAttributeValue> ItemVariantAttributeValues { get; set; }

        public DbSet<UserTargetType> UserTargetTypes { get; set; }

        public DbSet<UserTarget> UserTargets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Quotation>()
                .HasIndex(q => q.QuotationNumber);

            modelBuilder.Entity<Quotation>()
                .HasIndex(q => new { q.CompanyCode, q.FiscalYearLabel, q.SequenceNumber });

            modelBuilder.Entity<QuotationFiscalSequence>()
                .HasIndex(s => new { s.CompanyCode, s.FiscalYearLabel })
                .IsUnique();

            modelBuilder.Entity<QuotationLineItem>()
                .HasOne(li => li.Quotation)
                .WithMany(q => q.LineItems)
                .HasForeignKey(li => li.QuotationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany()
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.RoleId);

            modelBuilder.Entity<User>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(u => u.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<User>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(u => u.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

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

            modelBuilder.Entity<DealStatus>()
                .HasIndex(s => s.Name)
                .IsUnique();

            modelBuilder.Entity<RequestType>()
                .HasIndex(r => r.Name)
                .IsUnique();

            modelBuilder.Entity<Role>()
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
                .HasOne(l => l.LeadOwner)
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

            modelBuilder.Entity<Lead>()
                .Property(l => l.LeadDate)
                .HasColumnType("date");

            modelBuilder.Entity<LeadHistory>()
                .Property(h => h.LeadDate)
                .HasColumnType("date");

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
                .HasOne(d => d.DealStatus)
                .WithMany()
                .HasForeignKey(d => d.DealStatusId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Deal>()
                .HasIndex(d => d.DealStatusId);

            modelBuilder.Entity<DealStageHistory>()
                .HasIndex(h => new { h.DealId, h.ChangedAt });

            modelBuilder.Entity<DealStageHistory>()
                .HasOne<Deal>()
                .WithMany()
                .HasForeignKey(h => h.DealId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DealStageHistory>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(h => h.ChangedByUserId)
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
                .HasOne(d => d.DealOwner)
                .WithMany()
                .HasForeignKey(d => d.DealOwnerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Deal>()
                .HasOne(d => d.AssignedToUser)
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

            modelBuilder.Entity<Comment>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Comment>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(c => c.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Comment>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(c => c.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Comment>()
                .HasIndex(c => new { c.EntityType, c.EntityId });

            modelBuilder.Entity<Email>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.SentBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Email>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Email>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Email>()
                .HasIndex(e => new { e.EntityType, e.EntityId });

            modelBuilder.Entity<ActivityLog>()
                .HasIndex(a => new { a.EntityType, a.EntityId, a.CreatedAt });

            modelBuilder.Entity<Organization>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(o => o.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Organization>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(o => o.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Contact>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(c => c.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Contact>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(c => c.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Lead>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(l => l.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Lead>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(l => l.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<LeadHistory>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(h => h.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<LeadHistory>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(h => h.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Deal>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Deal>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Note>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(n => n.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Note>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(n => n.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TaskTable>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<TaskTable>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CallLog>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(c => c.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<CallLog>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(c => c.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Salutation>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(s => s.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Salutation>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(s => s.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<EmployeeCount>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<EmployeeCount>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Territory>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Territory>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Industry>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(i => i.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Industry>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(i => i.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<LeadStatus>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(s => s.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<LeadStatus>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(s => s.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DealStatus>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(s => s.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<DealStatus>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(s => s.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<RequestType>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(r => r.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<RequestType>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(r => r.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Role>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(r => r.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Role>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(r => r.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Permission>()
                .HasIndex(p => p.Code)
                .IsUnique();

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany()
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany()
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Integer PKs: PostgreSQL GENERATED ALWAYS AS IDENTITY (values come from the database only).
            modelBuilder.Entity<Salutation>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<EmployeeCount>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<Territory>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<Industry>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<LeadStatus>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<DealStatus>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<RequestType>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<Role>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<Permission>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<User>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<Organization>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<Contact>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<Lead>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<LeadHistory>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<Deal>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<DealStageHistory>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<Note>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<TaskTable>().Property(e => e.TaskId).UseIdentityAlwaysColumn();
            modelBuilder.Entity<CallLog>().Property(e => e.CallId).UseIdentityAlwaysColumn();
            modelBuilder.Entity<ActivityLog>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<Comment>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<Email>().Property(e => e.Id).UseIdentityAlwaysColumn();

            modelBuilder.Entity<ItemGroup>()
                .HasOne(g => g.Parent)
                .WithMany(g => g.Children)
                .HasForeignKey(g => g.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ItemGroup>()
                .HasIndex(g => g.Name);

            modelBuilder.Entity<ItemAttribute>()
                .HasIndex(a => a.Code)
                .IsUnique();

            modelBuilder.Entity<ItemAttributeValue>()
                .HasOne(v => v.Attribute)
                .WithMany(a => a.Values)
                .HasForeignKey(v => v.AttributeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Item>()
                .HasIndex(i => i.ItemCode)
                .IsUnique();

            modelBuilder.Entity<Item>()
                .HasOne(i => i.ItemGroup)
                .WithMany()
                .HasForeignKey(i => i.ItemGroupId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Item>()
                .HasOne(i => i.ParentItem)
                .WithMany(i => i.Variants)
                .HasForeignKey(i => i.ParentItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ItemSpecification>()
                .HasOne(s => s.Item)
                .WithMany(i => i.Specifications)
                .HasForeignKey(s => s.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ItemTemplateAttribute>()
                .HasOne(t => t.Item)
                .WithMany(i => i.TemplateAttributes)
                .HasForeignKey(t => t.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ItemTemplateAttribute>()
                .HasOne(t => t.Attribute)
                .WithMany()
                .HasForeignKey(t => t.AttributeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ItemTemplateAttribute>()
                .HasIndex(t => new { t.ItemId, t.AttributeId })
                .IsUnique();

            modelBuilder.Entity<ItemVariantAttributeValue>()
                .HasOne(v => v.Item)
                .WithMany(i => i.VariantAttributeValues)
                .HasForeignKey(v => v.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ItemVariantAttributeValue>()
                .HasOne(v => v.Attribute)
                .WithMany()
                .HasForeignKey(v => v.AttributeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ItemVariantAttributeValue>()
                .HasOne(v => v.AttributeValue)
                .WithMany()
                .HasForeignKey(v => v.AttributeValueId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ItemGroup>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(g => g.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ItemGroup>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(g => g.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ItemAttribute>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ItemAttribute>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Item>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(i => i.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Item>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(i => i.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ItemGroup>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<ItemAttribute>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<ItemAttributeValue>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<Item>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<ItemSpecification>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<ItemTemplateAttribute>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<ItemVariantAttributeValue>().Property(e => e.Id).UseIdentityAlwaysColumn();

            modelBuilder.Entity<UserTargetType>()
                .HasIndex(t => t.Name)
                .IsUnique();

            modelBuilder.Entity<UserTargetType>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<UserTargetType>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<UserTarget>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserTarget>()
                .HasOne(t => t.TargetType)
                .WithMany()
                .HasForeignKey(t => t.TargetTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserTarget>()
                .HasIndex(t => new { t.UserId, t.TargetTypeId, t.StartDate, t.EndDate });

            modelBuilder.Entity<UserTarget>()
                .HasIndex(t => t.IsActive);

            modelBuilder.Entity<UserTarget>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<UserTarget>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<UserTargetType>().Property(e => e.Id).UseIdentityAlwaysColumn();
            modelBuilder.Entity<UserTarget>().Property(e => e.Id).UseIdentityAlwaysColumn();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            try
            {
                AppendLeadHistorySnapshots();
                AppendDealStageHistoryRecords();
                StampAuditTimestamps();
                var activityBatch = ActivityCapture.Capture(this);
                var result = base.SaveChanges(acceptAllChangesOnSuccess);
                ActivityCapture.Flush(this, activityBatch);
                if (ChangeTracker.HasChanges())
                {
                    result += base.SaveChanges(acceptAllChangesOnSuccess);
                }

                return result;
            }
            finally
            {
                AuditUserId = null;
                DealStageChangeComment = null;
            }
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            try
            {
                AppendLeadHistorySnapshots();
                AppendDealStageHistoryRecords();
                StampAuditTimestamps();
                var activityBatch = ActivityCapture.Capture(this);
                var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
                ActivityCapture.Flush(this, activityBatch);
                if (ChangeTracker.HasChanges())
                {
                    result += await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
                }

                return result;
            }
            finally
            {
                AuditUserId = null;
                DealStageChangeComment = null;
            }
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
                    Location = (string)o[nameof(Lead.Location)]!,
                    LeadDate = (DateTime?)o[nameof(Lead.LeadDate)],
                    LeadOwnerId = (int?)o[nameof(Lead.LeadOwnerId)],
                    LeadSource = (string)o[nameof(Lead.LeadSource)]!,
                    IsActive = (bool)o[nameof(Lead.IsActive)]!,
                    CreatedAt = (DateTime?)o[nameof(Lead.CreatedAt)],
                    UpdatedAt = (DateTime)o[nameof(Lead.UpdatedAt)]!,
                });
            }
        }

        /// <summary>Records deal pipeline stage transitions when <see cref="Deal.DealStatusId"/> changes.</summary>
        private void AppendDealStageHistoryRecords()
        {
            var utc = DateTime.UtcNow;
            foreach (var entry in ChangeTracker.Entries<Deal>())
            {
                if (entry.State != EntityState.Modified)
                {
                    continue;
                }

                var statusProp = entry.Property(d => d.DealStatusId);
                if (!statusProp.IsModified)
                {
                    continue;
                }

                var oldId = statusProp.OriginalValue;
                var newId = statusProp.CurrentValue;
                if (oldId == newId)
                {
                    continue;
                }

                var o = entry.OriginalValues;
                var previousStage = (string)o[nameof(Deal.Status)]!;
                if (string.IsNullOrWhiteSpace(previousStage) && oldId is int oldStatusId && oldStatusId > 0)
                {
                    previousStage = DealStatuses.Local
                        .FirstOrDefault(s => s.Id == oldStatusId)?.Name ?? oldStatusId.ToString();
                }

                var newStage = entry.Entity.Status;
                if (string.IsNullOrWhiteSpace(newStage) && newId is int newStatusId && newStatusId > 0)
                {
                    newStage = DealStatuses.Local
                        .FirstOrDefault(s => s.Id == newStatusId)?.Name ?? newStatusId.ToString();
                }

                DealStageHistories.Add(new DealStageHistory
                {
                    DealId = entry.Entity.Id,
                    PreviousStage = previousStage ?? string.Empty,
                    NewStage = newStage ?? string.Empty,
                    ChangedByUserId = AuditUserId,
                    ChangedAt = utc,
                    Comment = DealStageChangeComment?.Trim() ?? string.Empty,
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
                        if (entry.Entity is IAuditableByUser addedAuditable && AuditUserId.HasValue)
                        {
                            addedAuditable.CreatedBy = AuditUserId;
                            addedAuditable.UpdatedBy = AuditUserId;
                        }

                        switch (entry.Entity)
                        {
                            case IMasterDataEntity master:
                                master.CreatedAt = utc;
                                master.UpdatedAt = utc;
                                master.LastModified = utc;
                                break;
                            case Organization o:
                                o.CreatedAt = utc;
                                o.UpdatedAt = utc;
                                o.LastModified = utc;
                                break;
                            case Contact c:
                                c.CreatedAt = utc;
                                c.UpdatedAt = utc;
                                c.LastModified = utc;
                                break;
                            case Deal d:
                                d.CreatedAt = utc;
                                d.UpdatedAt = utc;
                                d.LastModified = utc;
                                break;
                            case TaskTable t:
                                t.CreatedAt = utc;
                                t.UpdatedAt = utc;
                                t.LastModified = utc;
                                break;
                            case CallLog cl:
                                cl.CreatedAt = utc;
                                cl.UpdatedAt = utc;
                                cl.LastModified = utc;
                                break;
                            case Note n:
                                n.CreatedAt = utc;
                                n.UpdatedAt = utc;
                                break;
                            case Lead l:
                                l.UpdatedAt = utc;
                                if (!l.CreatedAt.HasValue)
                                {
                                    l.CreatedAt = utc;
                                }

                                break;
                            case User u:
                                if (u.UpdatedAt == default)
                                {
                                    u.UpdatedAt = u.CreatedAt != default ? u.CreatedAt : utc;
                                }

                                break;
                            case Comment cm:
                                cm.CreatedAt = utc;
                                cm.UpdatedAt = utc;
                                break;
                            case Email em:
                                em.CreatedAt = utc;
                                em.UpdatedAt = utc;
                                break;
                            case Quotation qt:
                                qt.CreatedAt = utc;
                                qt.UpdatedAt = utc;
                                break;
                            case ItemGroup ig:
                                ig.CreatedAt = utc;
                                ig.UpdatedAt = utc;
                                break;
                            case ItemAttribute ia:
                                ia.CreatedAt = utc;
                                ia.UpdatedAt = utc;
                                break;
                            case Item it:
                                it.CreatedAt = utc;
                                it.UpdatedAt = utc;
                                break;
                            case UserTarget ut:
                                ut.CreatedAt = utc;
                                ut.UpdatedAt = utc;
                                ut.LastModified = utc;
                                break;
                        }

                        break;
                    case EntityState.Modified:
                        if (entry.Entity is IAuditableByUser modifiedAuditable && AuditUserId.HasValue)
                        {
                            modifiedAuditable.UpdatedBy = AuditUserId;
                        }

                        switch (entry.Entity)
                        {
                            case IMasterDataEntity master:
                                master.UpdatedAt = utc;
                                master.LastModified = utc;
                                break;
                            case Organization o:
                                o.UpdatedAt = utc;
                                o.LastModified = utc;
                                break;
                            case Contact c:
                                c.UpdatedAt = utc;
                                c.LastModified = utc;
                                break;
                            case Deal d:
                                d.UpdatedAt = utc;
                                d.LastModified = utc;
                                break;
                            case TaskTable t:
                                t.UpdatedAt = utc;
                                t.LastModified = utc;
                                break;
                            case CallLog cl:
                                cl.UpdatedAt = utc;
                                cl.LastModified = utc;
                                break;
                            case Note n:
                                n.UpdatedAt = utc;
                                break;
                            case Lead l:
                                l.UpdatedAt = utc;
                                break;
                            case User u:
                                u.UpdatedAt = utc;
                                break;
                            case Comment cm:
                                cm.UpdatedAt = utc;
                                break;
                            case Email em:
                                em.UpdatedAt = utc;
                                break;
                            case Quotation qt:
                                qt.UpdatedAt = utc;
                                break;
                            case ItemGroup ig:
                                ig.UpdatedAt = utc;
                                break;
                            case ItemAttribute ia:
                                ia.UpdatedAt = utc;
                                break;
                            case Item it:
                                it.UpdatedAt = utc;
                                break;
                            case UserTarget ut:
                                ut.UpdatedAt = utc;
                                ut.LastModified = utc;
                                break;
                        }

                        break;
                }
            }
        }
    }
}
