using HRMS.Authorization;
using HRMS.Data;
using HRMS.DTOs;
using HRMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Controllers;

[Route("api/recruitment")]
[ApiController]
public class RecruitmentController : ControllerBase
{
    private readonly HRMSDbContext _context;
    private readonly ICurrentUserAccessor _currentUser;

    public RecruitmentController(HRMSDbContext context, ICurrentUserAccessor currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    // =========================================================================
    // 1. JOB REQUISITIONS
    // =========================================================================
    [HttpGet("requisitions")]
    public async Task<IActionResult> GetRequisitions([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var query = _context.JobRequisitions.AsNoTracking()
            .Include(r => r.Department)
            .Include(r => r.Branch)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        var list = await query.OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
        return Ok(list.Select(r => new JobRequisitionDto
        {
            Id = r.Id,
            Title = r.Title,
            DepartmentId = r.DepartmentId,
            DepartmentName = r.Department?.Name,
            BranchId = r.BranchId,
            BranchName = r.Branch?.Name,
            OpeningsCount = r.OpeningsCount,
            MinExperienceYears = r.MinExperienceYears,
            JobDescription = r.JobDescription,
            TargetDate = r.TargetDate,
            Status = r.Status,
            CreatedAt = r.CreatedAt
        }));
    }

    [HttpPost("requisitions")]
    [RequirePermission(HrmsPermissions.ManageEmployees)]
    public async Task<IActionResult> CreateRequisition([FromBody] JobRequisitionUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var req = new JobRequisition
        {
            Title = dto.Title.Trim(),
            DepartmentId = dto.DepartmentId,
            BranchId = dto.BranchId,
            OpeningsCount = dto.OpeningsCount,
            MinExperienceYears = dto.MinExperienceYears,
            JobDescription = dto.JobDescription?.Trim(),
            TargetDate = dto.TargetDate,
            Status = dto.Status?.Trim() ?? "Open",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.JobRequisitions.Add(req);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(req);
    }

    [HttpPut("requisitions/{id:int}")]
    [RequirePermission(HrmsPermissions.ManageEmployees)]
    public async Task<IActionResult> UpdateRequisition(int id, [FromBody] JobRequisitionUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var req = await _context.JobRequisitions.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (req == null) return NotFound();

        req.Title = dto.Title.Trim();
        req.DepartmentId = dto.DepartmentId;
        req.BranchId = dto.BranchId;
        req.OpeningsCount = dto.OpeningsCount;
        req.MinExperienceYears = dto.MinExperienceYears;
        req.JobDescription = dto.JobDescription?.Trim();
        req.TargetDate = dto.TargetDate;
        req.Status = dto.Status?.Trim() ?? "Open";
        req.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(req);
    }

    // =========================================================================
    // 2. CANDIDATES
    // =========================================================================
    [HttpGet("candidates")]
    public async Task<IActionResult> GetCandidates(
        [FromQuery] int? requisitionId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var query = _context.Candidates.AsNoTracking()
            .Include(c => c.JobRequisition)
            .Include(c => c.Department)
            .Include(c => c.JoinedEmployee)
            .AsQueryable();

        if (requisitionId.HasValue) query = query.Where(c => c.JobRequisitionId == requisitionId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(c => c.Status == status);

        var list = await query.OrderByDescending(c => c.CreatedAt).ToListAsync(cancellationToken);
        return Ok(list.Select(c => new CandidateDto
        {
            Id = c.Id,
            JobRequisitionId = c.JobRequisitionId,
            JobTitle = c.JobRequisition?.Title,
            FullName = c.FullName,
            Email = c.Email,
            PhoneNumber = c.PhoneNumber,
            Position = c.Position,
            DepartmentId = c.DepartmentId,
            DepartmentName = c.Department?.Name,
            ExperienceYears = c.ExperienceYears,
            ResumePath = c.ResumePath,
            Source = c.Source,
            Status = c.Status,
            Notes = c.Notes,
            JoinedEmployeeId = c.JoinedEmployeeId,
            JoinedEmployeeName = c.JoinedEmployee?.FullName,
            CreatedAt = c.CreatedAt
        }));
    }

    [HttpPost("candidates")]
    [RequirePermission(HrmsPermissions.ManageEmployees)]
    public async Task<IActionResult> CreateCandidate([FromBody] CandidateUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var candidate = new Candidate
        {
            JobRequisitionId = dto.JobRequisitionId,
            FullName = dto.FullName.Trim(),
            Email = dto.Email.Trim(),
            PhoneNumber = dto.PhoneNumber.Trim(),
            Position = dto.Position.Trim(),
            DepartmentId = dto.DepartmentId,
            ExperienceYears = dto.ExperienceYears,
            ResumePath = dto.ResumePath?.Trim(),
            Source = dto.Source?.Trim() ?? "Job Portal",
            Status = dto.Status?.Trim() ?? "APPLIED",
            Notes = dto.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Candidates.Add(candidate);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(candidate);
    }

    [HttpPut("candidates/{id:int}/status")]
    [RequirePermission(HrmsPermissions.ManageEmployees)]
    public async Task<IActionResult> UpdateCandidateStatus(int id, [FromBody] CandidateStatusUpdateDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var candidate = await _context.Candidates.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (candidate == null) return NotFound();

        candidate.Status = dto.Status.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(dto.Notes)) candidate.Notes = dto.Notes.Trim();
        if (dto.JoinedEmployeeId.HasValue) candidate.JoinedEmployeeId = dto.JoinedEmployeeId.Value;
        candidate.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(candidate);
    }

    // =========================================================================
    // 3. ONBOARDING TASKS
    // =========================================================================
    [HttpGet("onboarding-tasks")]
    public async Task<IActionResult> GetOnboardingTasks([FromQuery] int? employeeId, CancellationToken cancellationToken)
    {
        var query = _context.OnboardingTasks.AsNoTracking().Include(t => t.Employee).AsQueryable();
        if (employeeId.HasValue) query = query.Where(t => t.EmployeeId == employeeId.Value || t.EmployeeId == null);

        var list = await query.OrderBy(t => t.Category).ThenBy(t => t.Id).ToListAsync(cancellationToken);

        if (list.Count == 0)
        {
            // Seed default checklist for employee if none exist
            var defaults = new[]
            {
                new OnboardingTask { EmployeeId = employeeId, TaskName = "Complete Employee Profile Information", Category = "Profile", IsMandatory = true },
                new OnboardingTask { EmployeeId = employeeId, TaskName = "Submit Government Identity Proof (PAN / Aadhaar)", Category = "Identity Documents", IsMandatory = true },
                new OnboardingTask { EmployeeId = employeeId, TaskName = "Submit Address Proof Documents", Category = "Address Proof", IsMandatory = true },
                new OnboardingTask { EmployeeId = employeeId, TaskName = "Submit Educational Certificates & Degrees", Category = "Education", IsMandatory = true },
                new OnboardingTask { EmployeeId = employeeId, TaskName = "Submit Bank Account & Cancelled Cheque", Category = "Bank Details", IsMandatory = true },
                new OnboardingTask { EmployeeId = employeeId, TaskName = "Submit Tax Declaration & Form 12B/16", Category = "Tax Info", IsMandatory = false },
                new OnboardingTask { EmployeeId = employeeId, TaskName = "Acknowledge Company Policies & Code of Conduct", Category = "Policy Agreement", IsMandatory = true },
                new OnboardingTask { EmployeeId = employeeId, TaskName = "Hardware & Laptop Asset Allocation", Category = "Asset Allocation", IsMandatory = true },
                new OnboardingTask { EmployeeId = employeeId, TaskName = "Email & Cloud System Access Provisioning", Category = "System Access", IsMandatory = true },
                new OnboardingTask { EmployeeId = employeeId, TaskName = "Reporting Manager & Team Introduction", Category = "Manager Intro", IsMandatory = true }
            };

            _context.OnboardingTasks.AddRange(defaults);
            await _context.SaveChangesAsync(cancellationToken);

            list = defaults.ToList();
        }

        return Ok(list.Select(t => new OnboardingTaskDto
        {
            Id = t.Id,
            EmployeeId = t.EmployeeId,
            EmployeeName = t.Employee?.FullName,
            TaskName = t.TaskName,
            Category = t.Category,
            IsMandatory = t.IsMandatory,
            IsCompleted = t.IsCompleted,
            CompletedAt = t.CompletedAt,
            Notes = t.Notes,
            CreatedAt = t.CreatedAt
        }));
    }

    [HttpPost("onboarding-tasks")]
    [RequirePermission(HrmsPermissions.ManageEmployees)]
    public async Task<IActionResult> CreateOnboardingTask([FromBody] OnboardingTaskUpsertDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var task = new OnboardingTask
        {
            EmployeeId = dto.EmployeeId,
            TaskName = dto.TaskName.Trim(),
            Category = dto.Category?.Trim() ?? "Profile",
            IsMandatory = dto.IsMandatory,
            IsCompleted = dto.IsCompleted,
            CompletedAt = dto.IsCompleted ? DateTime.UtcNow : null,
            Notes = dto.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.OnboardingTasks.Add(task);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(task);
    }

    [HttpPut("onboarding-tasks/{id:int}/toggle")]
    [RequirePermission(HrmsPermissions.ManageEmployees)]
    public async Task<IActionResult> ToggleOnboardingTask(int id, CancellationToken cancellationToken)
    {
        var task = await _context.OnboardingTasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task == null) return NotFound();

        task.IsCompleted = !task.IsCompleted;
        task.CompletedAt = task.IsCompleted ? DateTime.UtcNow : null;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(task);
    }
}
