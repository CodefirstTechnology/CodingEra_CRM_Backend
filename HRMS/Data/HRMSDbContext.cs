using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Data;

public class HRMSDbContext : DbContext
{
    public HRMSDbContext(DbContextOptions<HRMSDbContext> options)
        : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Designation> Designations => Set<Designation>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveRejectionReason> LeaveRejectionReasons => Set<LeaveRejectionReason>();
    public DbSet<DocumentCategory> DocumentCategories => Set<DocumentCategory>();
    public DbSet<Employee> Employees => Set<Employee>();
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
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasData(
                new Department { Id = 1, Name = "Engineering", IsActive = true },
                new Department { Id = 2, Name = "Human Resources", IsActive = true },
                new Department { Id = 3, Name = "Sales", IsActive = true });
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasData(
                new Branch { Id = 1, Name = "Headquarters - Mumbai", IsActive = true },
                new Branch { Id = 2, Name = "Branch - Pune", IsActive = true },
                new Branch { Id = 3, Name = "Branch - Bangalore", IsActive = true });
        });

        modelBuilder.Entity<Designation>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasData(
                new Designation { Id = 1, Name = "Senior Full Stack Developer", IsActive = true },
                new Designation { Id = 2, Name = "HR Lead", IsActive = true },
                new Designation { Id = 3, Name = "Sales Executive", IsActive = true });
        });

        modelBuilder.Entity<LeaveType>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasData(
                new LeaveType { Id = 1, Name = "Casual Leave", Code = "CL", DefaultAllocatedDays = 12, IsActive = true },
                new LeaveType { Id = 2, Name = "Sick Leave", Code = "SL", DefaultAllocatedDays = 10, IsActive = true },
                new LeaveType { Id = 3, Name = "Earned Leave", Code = "EL", DefaultAllocatedDays = 15, IsActive = true });
        });

        modelBuilder.Entity<LeaveRejectionReason>(entity =>
        {
            entity.HasIndex(e => e.Title).IsUnique();
            entity.HasData(
                new LeaveRejectionReason { Id = 1, Title = "Project Delivery Deadline / Critical Milestone", IsActive = true, SortOrder = 1 },
                new LeaveRejectionReason { Id = 2, Title = "Insufficient Leave Balance", IsActive = true, SortOrder = 2 },
                new LeaveRejectionReason { Id = 3, Title = "Team Resource Shortage on Requested Dates", IsActive = true, SortOrder = 3 },
                new LeaveRejectionReason { Id = 4, Title = "Overlapping Team Member Leave", IsActive = true, SortOrder = 4 },
                new LeaveRejectionReason { Id = 5, Title = "Incomplete Information / Missing Attachments", IsActive = true, SortOrder = 5 },
                new LeaveRejectionReason { Id = 6, Title = "Other Reason", IsActive = true, SortOrder = 6 });
        });

        modelBuilder.Entity<DocumentCategory>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasData(
                new DocumentCategory { Id = 1, Name = "Identity (Aadhaar)", IsActive = true },
                new DocumentCategory { Id = 2, Name = "PAN", IsActive = true },
                new DocumentCategory { Id = 3, Name = "Education", IsActive = true });
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.EmployeeCode).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DepartmentId);
            entity.HasIndex(e => e.BranchId);
        });

        modelBuilder.Entity<AttendanceRecord>(entity =>
        {
            entity.HasIndex(e => new { e.EmployeeId, e.AttendanceDate }).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.AttendanceDate);
        });

        modelBuilder.Entity<LeaveAllocation>(entity =>
        {
            entity.HasIndex(e => new { e.EmployeeId, e.LeaveTypeId, e.Year }).IsUnique();
        });

        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasOne(e => e.ApprovedByUser)
                .WithMany()
                .HasForeignKey(e => e.ApprovedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<LeaveNotification>(entity =>
        {
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.IsRead);
        });

        modelBuilder.Entity<PayrollRecord>(entity =>
        {
            entity.HasIndex(e => new { e.EmployeeId, e.PayMonth, e.PayYear }).IsUnique();
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<EmployeeDocument>(entity =>
        {
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.DocumentCategoryId);
        });

        modelBuilder.Entity<PerformanceReview>(entity =>
        {
            entity.HasIndex(e => new { e.EmployeeId, e.ReviewPeriod }).IsUnique();
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.RoleId);
            entity.HasOne(e => e.Role)
                .WithMany()
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
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
}
