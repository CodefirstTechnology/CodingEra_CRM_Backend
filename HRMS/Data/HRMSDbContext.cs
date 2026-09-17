using HRMS.Authorization;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Data;

public class HRMSDbContext : DbContext
{
    private readonly ITenantAccessor? _tenantAccessor;
    private readonly ICurrentUserAccessor? _currentUserAccessor;

    public HRMSDbContext(
        DbContextOptions<HRMSDbContext> options,
        ITenantAccessor? tenantAccessor = null,
        ICurrentUserAccessor? currentUserAccessor = null)
        : base(options)
    {
        _tenantAccessor = tenantAccessor;
        _currentUserAccessor = currentUserAccessor;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<CompanyProfile> CompanyProfiles => Set<CompanyProfile>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Designation> Designations => Set<Designation>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<WorkingDayConfig> WorkingDayConfigs => Set<WorkingDayConfig>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeTransfer> EmployeeTransfers => Set<EmployeeTransfer>();
    public DbSet<EmployeePromotion> EmployeePromotions => Set<EmployeePromotion>();
    public DbSet<SalaryRevisionRequest> SalaryRevisionRequests => Set<SalaryRevisionRequest>();
    public DbSet<EmployeeExit> EmployeeExits => Set<EmployeeExit>();
    public DbSet<EmployeeLifecycleHistory> EmployeeLifecycleHistories => Set<EmployeeLifecycleHistory>();
    public DbSet<JobRequisition> JobRequisitions => Set<JobRequisition>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<OnboardingTask> OnboardingTasks => Set<OnboardingTask>();
    public DbSet<CompanyAsset> CompanyAssets => Set<CompanyAsset>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveRejectionReason> LeaveRejectionReasons => Set<LeaveRejectionReason>();
    public DbSet<DocumentCategory> DocumentCategories => Set<DocumentCategory>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<LeaveAllocation> LeaveAllocations => Set<LeaveAllocation>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveNotification> LeaveNotifications => Set<LeaveNotification>();
    public DbSet<PayrollRecord> PayrollRecords => Set<PayrollRecord>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<PerformanceReview> PerformanceReviews => Set<PerformanceReview>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Action);
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId || e.TenantId == null);
        });

        modelBuilder.Entity<CompanyProfile>(entity =>
        {
            entity.HasIndex(e => e.TenantId).IsUnique();
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Name });
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Name });
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<Designation>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Name });
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<Grade>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.GradeCode });
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<CostCenter>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.CostCenterCode });
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<Shift>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.ShiftCode });
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<Holiday>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.HolidayDate });
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<WorkingDayConfig>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.DayOfWeek });
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.EmployeeCode });
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DepartmentId);
            entity.HasIndex(e => e.BranchId);
            entity.HasIndex(e => e.ReportingManagerId);
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<EmployeeTransfer>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.EmployeeId });
            entity.HasIndex(e => e.Status);
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<EmployeePromotion>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.EmployeeId });
            entity.HasIndex(e => e.Status);
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<SalaryRevisionRequest>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.EmployeeId });
            entity.HasIndex(e => e.Status);
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<EmployeeExit>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.EmployeeId });
            entity.HasIndex(e => e.Status);
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<EmployeeLifecycleHistory>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.EmployeeId });
            entity.HasIndex(e => e.CreatedAt);
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<JobRequisition>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<Candidate>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasIndex(e => e.Email);
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<OnboardingTask>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.EmployeeId });
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<CompanyAsset>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.AssetCode }).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.AssignedToEmployeeId);
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<LeaveType>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Code });
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<LeaveRejectionReason>(entity =>
        {
            entity.HasIndex(e => e.Title);
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId || e.TenantId == null);
        });

        modelBuilder.Entity<DocumentCategory>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Name });
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<AttendanceRecord>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.EmployeeId, e.AttendanceDate });
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.AttendanceDate);
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<LeaveAllocation>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.EmployeeId, e.LeaveTypeId, e.Year });
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasOne(e => e.ApprovedByUser)
                .WithMany()
                .HasForeignKey(e => e.ApprovedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<LeaveNotification>(entity =>
        {
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.IsRead);
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<PayrollRecord>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.EmployeeId, e.PayMonth, e.PayYear });
            entity.HasIndex(e => e.Status);
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<EmployeeDocument>(entity =>
        {
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.DocumentCategoryId);
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<PerformanceReview>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.EmployeeId, e.ReviewPeriod });
            entity.HasIndex(e => e.Status);
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.RoleId);
            entity.HasIndex(e => e.TenantId);
            entity.HasOne(e => e.Role)
                .WithMany()
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => _tenantAccessor == null || !_tenantAccessor.HasTenant || e.TenantId == _tenantAccessor.TenantId || e.TenantId == null);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasData(
                RoleSeed.SuperAdmin,
                RoleSeed.HrAdmin,
                RoleSeed.Employee);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries();
        var now = DateTime.UtcNow;
        var currentUserId = _currentUserAccessor?.UserId;
        var currentTenantId = _tenantAccessor?.TenantId;

        foreach (var entry in entries)
        {
            if (entry.Entity is ITenantEntity tenantEntity)
            {
                if (tenantEntity.TenantId <= 0 && currentTenantId.HasValue && currentTenantId.Value > 0)
                {
                    tenantEntity.TenantId = currentTenantId.Value;
                }
            }

            if (entry.Entity is IAuditableEntity auditableEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    if (auditableEntity.CreatedAt == default)
                    {
                        auditableEntity.CreatedAt = now;
                    }
                    auditableEntity.UpdatedAt = now;
                    if (currentUserId.HasValue)
                    {
                        auditableEntity.CreatedBy ??= currentUserId.Value;
                        auditableEntity.UpdatedBy ??= currentUserId.Value;
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditableEntity.UpdatedAt = now;
                    if (currentUserId.HasValue)
                    {
                        auditableEntity.UpdatedBy = currentUserId.Value;
                    }
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
