using HRMS.Authorization;
using HRMS.Data;
using HRMS.DTOs;
using HRMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Controllers;

[Route("api/employee-lifecycle")]
[ApiController]
public class EmployeeLifecycleController : ControllerBase
{
    private readonly HRMSDbContext _context;
    private readonly ICurrentUserAccessor _currentUser;

    public EmployeeLifecycleController(HRMSDbContext context, ICurrentUserAccessor currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    // =========================================================================
    // 1. TRANSFERS
    // =========================================================================
    [HttpGet("transfers")]
    public async Task<IActionResult> GetTransfers([FromQuery] int? employeeId, CancellationToken cancellationToken)
    {
        var query = _context.EmployeeTransfers.AsNoTracking()
            .Include(t => t.Employee)
            .AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(t => t.EmployeeId == employeeId.Value);
        }

        var list = await query.OrderByDescending(t => t.CreatedAt).ToListAsync(cancellationToken);

        // Fetch names for branch, dept, designation, manager
        var branchIds = list.SelectMany(t => new[] { t.CurrentBranchId, t.NewBranchId }).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var deptIds = list.SelectMany(t => new[] { t.CurrentDepartmentId, t.NewDepartmentId }).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var desigIds = list.SelectMany(t => new[] { t.CurrentDesignationId, t.NewDesignationId }).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var mgrIds = list.SelectMany(t => new[] { t.CurrentReportingManagerId, t.NewReportingManagerId }).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var costIds = list.SelectMany(t => new[] { t.CurrentCostCenterId, t.NewCostCenterId }).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();

        var branches = await _context.Branches.Where(b => branchIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.Name, cancellationToken);
        var depts = await _context.Departments.Where(d => deptIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken);
        var desigs = await _context.Designations.Where(d => desigIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken);
        var mgrs = await _context.Employees.Where(e => mgrIds.Contains(e.Id)).ToDictionaryAsync(e => e.Id, e => e.FullName, cancellationToken);
        var costs = await _context.CostCenters.Where(c => costIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.CostCenterName, cancellationToken);

        var dtos = list.Select(t => new EmployeeTransferDto
        {
            Id = t.Id,
            EmployeeId = t.EmployeeId,
            EmployeeName = t.Employee?.FullName ?? string.Empty,
            EmployeeCode = t.Employee?.EmployeeCode ?? string.Empty,
            CurrentBranchId = t.CurrentBranchId,
            CurrentBranchName = t.CurrentBranchId.HasValue && branches.ContainsKey(t.CurrentBranchId.Value) ? branches[t.CurrentBranchId.Value] : null,
            NewBranchId = t.NewBranchId,
            NewBranchName = t.NewBranchId.HasValue && branches.ContainsKey(t.NewBranchId.Value) ? branches[t.NewBranchId.Value] : null,
            CurrentDepartmentId = t.CurrentDepartmentId,
            CurrentDepartmentName = t.CurrentDepartmentId.HasValue && depts.ContainsKey(t.CurrentDepartmentId.Value) ? depts[t.CurrentDepartmentId.Value] : null,
            NewDepartmentId = t.NewDepartmentId,
            NewDepartmentName = t.NewDepartmentId.HasValue && depts.ContainsKey(t.NewDepartmentId.Value) ? depts[t.NewDepartmentId.Value] : null,
            CurrentDesignationId = t.CurrentDesignationId,
            CurrentDesignationName = t.CurrentDesignationId.HasValue && desigs.ContainsKey(t.CurrentDesignationId.Value) ? desigs[t.CurrentDesignationId.Value] : null,
            NewDesignationId = t.NewDesignationId,
            NewDesignationName = t.NewDesignationId.HasValue && desigs.ContainsKey(t.NewDesignationId.Value) ? desigs[t.NewDesignationId.Value] : null,
            CurrentReportingManagerId = t.CurrentReportingManagerId,
            CurrentReportingManagerName = t.CurrentReportingManagerId.HasValue && mgrs.ContainsKey(t.CurrentReportingManagerId.Value) ? mgrs[t.CurrentReportingManagerId.Value] : null,
            NewReportingManagerId = t.NewReportingManagerId,
            NewReportingManagerName = t.NewReportingManagerId.HasValue && mgrs.ContainsKey(t.NewReportingManagerId.Value) ? mgrs[t.NewReportingManagerId.Value] : null,
            CurrentCostCenterId = t.CurrentCostCenterId,
            CurrentCostCenterName = t.CurrentCostCenterId.HasValue && costs.ContainsKey(t.CurrentCostCenterId.Value) ? costs[t.CurrentCostCenterId.Value] : null,
            NewCostCenterId = t.NewCostCenterId,
            NewCostCenterName = t.NewCostCenterId.HasValue && costs.ContainsKey(t.NewCostCenterId.Value) ? costs[t.NewCostCenterId.Value] : null,
            EffectiveDate = t.EffectiveDate,
            Reason = t.Reason,
            Remarks = t.Remarks,
            RequestedByUserId = t.RequestedByUserId,
            ApprovedByUserId = t.ApprovedByUserId,
            Status = t.Status,
            CreatedAt = t.CreatedAt
        }).ToList();

        return Ok(dtos);
    }

    [HttpPost("transfers")]
    [RequirePermission(HrmsPermissions.ManageEmployees)]
    public async Task<IActionResult> CreateTransfer([FromBody] EmployeeTransferCreateDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == dto.EmployeeId, cancellationToken);
        if (employee == null) return NotFound("Employee not found.");

        var transfer = new EmployeeTransfer
        {
            EmployeeId = dto.EmployeeId,
            CurrentBranchId = employee.BranchId,
            NewBranchId = dto.NewBranchId ?? employee.BranchId,
            CurrentDepartmentId = employee.DepartmentId,
            NewDepartmentId = dto.NewDepartmentId ?? employee.DepartmentId,
            CurrentDesignationId = employee.DesignationId,
            NewDesignationId = dto.NewDesignationId ?? employee.DesignationId,
            CurrentReportingManagerId = employee.ReportingManagerId,
            NewReportingManagerId = dto.NewReportingManagerId ?? employee.ReportingManagerId,
            CurrentCostCenterId = employee.CostCenterId,
            NewCostCenterId = dto.NewCostCenterId ?? employee.CostCenterId,
            EffectiveDate = dto.EffectiveDate,
            Reason = dto.Reason.Trim(),
            Remarks = dto.Remarks?.Trim(),
            RequestedByUserId = _currentUser.UserId,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.EmployeeTransfers.Add(transfer);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(transfer);
    }

    [HttpPut("transfers/{id:int}/approve")]
    [RequirePermission(HrmsPermissions.ManageEmployees)]
    public async Task<IActionResult> ApproveTransfer(int id, CancellationToken cancellationToken)
    {
        var transfer = await _context.EmployeeTransfers.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (transfer == null) return NotFound("Transfer request not found.");

        if (transfer.Status == "Approved") return BadRequest("Transfer is already approved.");

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == transfer.EmployeeId, cancellationToken);
        if (employee == null) return NotFound("Employee not found.");

        transfer.Status = "Approved";
        transfer.ApprovedByUserId = _currentUser.UserId;
        transfer.UpdatedAt = DateTime.UtcNow;

        // Update employee organization placement
        if (transfer.NewBranchId.HasValue) employee.BranchId = transfer.NewBranchId.Value;
        if (transfer.NewDepartmentId.HasValue) employee.DepartmentId = transfer.NewDepartmentId.Value;
        if (transfer.NewDesignationId.HasValue) employee.DesignationId = transfer.NewDesignationId.Value;
        if (transfer.NewReportingManagerId.HasValue) employee.ReportingManagerId = transfer.NewReportingManagerId.Value;
        if (transfer.NewCostCenterId.HasValue) employee.CostCenterId = transfer.NewCostCenterId.Value;
        employee.UpdatedAt = DateTime.UtcNow;

        // Record history
        _context.EmployeeLifecycleHistories.Add(new EmployeeLifecycleHistory
        {
            EmployeeId = employee.Id,
            EventType = "Transfer Approved",
            OldValue = $"Branch: {transfer.CurrentBranchId}, Dept: {transfer.CurrentDepartmentId}, Desig: {transfer.CurrentDesignationId}",
            NewValue = $"Branch: {transfer.NewBranchId}, Dept: {transfer.NewDepartmentId}, Desig: {transfer.NewDesignationId}",
            Reason = transfer.Reason,
            ChangedByUserId = _currentUser.UserId,
            ChangedByName = _currentUser.FullName ?? "HR Admin",
            CreatedAt = DateTime.UtcNow
        });

        // Audit Log
        _context.AuditLogs.Add(new AuditLog
        {
            TenantId = employee.TenantId,
            UserId = _currentUser.UserId ?? 1,
            Action = "TRANSFER_EMPLOYEE",
            EntityName = "EmployeeTransfer",
            EntityId = transfer.Id.ToString(),
            Details = $"Approved transfer for employee #{employee.Id} ({employee.FullName})",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Transfer approved and applied successfully." });
    }

    [HttpPut("transfers/{id:int}/reject")]
    [RequirePermission(HrmsPermissions.ManageEmployees)]
    public async Task<IActionResult> RejectTransfer(int id, [FromBody] string? remarks, CancellationToken cancellationToken)
    {
        var transfer = await _context.EmployeeTransfers.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (transfer == null) return NotFound();

        transfer.Status = "Rejected";
        if (!string.IsNullOrWhiteSpace(remarks)) transfer.Remarks = remarks.Trim();
        transfer.ApprovedByUserId = _currentUser.UserId;
        transfer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Transfer request rejected." });
    }

    // =========================================================================
    // 2. PROMOTIONS
    // =========================================================================
    [HttpGet("promotions")]
    public async Task<IActionResult> GetPromotions([FromQuery] int? employeeId, CancellationToken cancellationToken)
    {
        var query = _context.EmployeePromotions.AsNoTracking().Include(p => p.Employee).AsQueryable();
        if (employeeId.HasValue) query = query.Where(p => p.EmployeeId == employeeId.Value);

        var list = await query.OrderByDescending(p => p.CreatedAt).ToListAsync(cancellationToken);

        var desigIds = list.SelectMany(p => new[] { p.CurrentDesignationId, p.NewDesignationId }).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var gradeIds = list.SelectMany(p => new[] { p.CurrentGradeId, p.NewGradeId }).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var deptIds = list.SelectMany(p => new[] { p.CurrentDepartmentId, p.NewDepartmentId }).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();

        var desigs = await _context.Designations.Where(d => desigIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken);
        var grades = await _context.Grades.Where(g => gradeIds.Contains(g.Id)).ToDictionaryAsync(g => g.Id, g => g.GradeName, cancellationToken);
        var depts = await _context.Departments.Where(d => deptIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken);

        var dtos = list.Select(p => new EmployeePromotionDto
        {
            Id = p.Id,
            EmployeeId = p.EmployeeId,
            EmployeeName = p.Employee?.FullName ?? string.Empty,
            EmployeeCode = p.Employee?.EmployeeCode ?? string.Empty,
            CurrentDesignationId = p.CurrentDesignationId,
            CurrentDesignationName = p.CurrentDesignationId.HasValue && desigs.ContainsKey(p.CurrentDesignationId.Value) ? desigs[p.CurrentDesignationId.Value] : null,
            NewDesignationId = p.NewDesignationId,
            NewDesignationName = p.NewDesignationId.HasValue && desigs.ContainsKey(p.NewDesignationId.Value) ? desigs[p.NewDesignationId.Value] : null,
            CurrentGradeId = p.CurrentGradeId,
            CurrentGradeName = p.CurrentGradeId.HasValue && grades.ContainsKey(p.CurrentGradeId.Value) ? grades[p.CurrentGradeId.Value] : null,
            NewGradeId = p.NewGradeId,
            NewGradeName = p.NewGradeId.HasValue && grades.ContainsKey(p.NewGradeId.Value) ? grades[p.NewGradeId.Value] : null,
            CurrentDepartmentId = p.CurrentDepartmentId,
            CurrentDepartmentName = p.CurrentDepartmentId.HasValue && depts.ContainsKey(p.CurrentDepartmentId.Value) ? depts[p.CurrentDepartmentId.Value] : null,
            NewDepartmentId = p.NewDepartmentId,
            NewDepartmentName = p.NewDepartmentId.HasValue && depts.ContainsKey(p.NewDepartmentId.Value) ? depts[p.NewDepartmentId.Value] : null,
            CurrentSalary = p.CurrentSalary,
            NewSalary = p.NewSalary,
            SalaryRevisionReference = p.SalaryRevisionReference,
            EffectiveDate = p.EffectiveDate,
            Reason = p.Reason,
            Remarks = p.Remarks,
            RequestedByUserId = p.RequestedByUserId,
            ApprovedByUserId = p.ApprovedByUserId,
            Status = p.Status,
            CreatedAt = p.CreatedAt
        }).ToList();

        return Ok(dtos);
    }

    [HttpPost("promotions")]
    [RequirePermission(HrmsPermissions.ManageEmployees)]
    public async Task<IActionResult> CreatePromotion([FromBody] EmployeePromotionCreateDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == dto.EmployeeId, cancellationToken);
        if (employee == null) return NotFound("Employee not found.");

        var promotion = new EmployeePromotion
        {
            EmployeeId = dto.EmployeeId,
            CurrentDesignationId = employee.DesignationId,
            NewDesignationId = dto.NewDesignationId ?? employee.DesignationId,
            CurrentGradeId = employee.GradeId,
            NewGradeId = dto.NewGradeId ?? employee.GradeId,
            CurrentDepartmentId = employee.DepartmentId,
            NewDepartmentId = dto.NewDepartmentId ?? employee.DepartmentId,
            CurrentSalary = employee.CurrentCtc,
            NewSalary = dto.NewSalary ?? employee.CurrentCtc,
            SalaryRevisionReference = dto.SalaryRevisionReference?.Trim(),
            EffectiveDate = dto.EffectiveDate,
            Reason = dto.Reason.Trim(),
            Remarks = dto.Remarks?.Trim(),
            RequestedByUserId = _currentUser.UserId,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.EmployeePromotions.Add(promotion);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(promotion);
    }

    [HttpPut("promotions/{id:int}/approve")]
    [RequirePermission(HrmsPermissions.ManageEmployees)]
    public async Task<IActionResult> ApprovePromotion(int id, CancellationToken cancellationToken)
    {
        var promotion = await _context.EmployeePromotions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (promotion == null) return NotFound();

        if (promotion.Status == "Approved") return BadRequest("Promotion is already approved.");

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == promotion.EmployeeId, cancellationToken);
        if (employee == null) return NotFound("Employee not found.");

        promotion.Status = "Approved";
        promotion.ApprovedByUserId = _currentUser.UserId;
        promotion.UpdatedAt = DateTime.UtcNow;

        if (promotion.NewDesignationId.HasValue) employee.DesignationId = promotion.NewDesignationId.Value;
        if (promotion.NewGradeId.HasValue) employee.GradeId = promotion.NewGradeId.Value;
        if (promotion.NewDepartmentId.HasValue) employee.DepartmentId = promotion.NewDepartmentId.Value;
        if (promotion.NewSalary.HasValue) employee.CurrentCtc = promotion.NewSalary.Value;
        employee.UpdatedAt = DateTime.UtcNow;

        _context.EmployeeLifecycleHistories.Add(new EmployeeLifecycleHistory
        {
            EmployeeId = employee.Id,
            EventType = "Promotion Approved",
            OldValue = $"Desig: {promotion.CurrentDesignationId}, Grade: {promotion.CurrentGradeId}, Salary: {promotion.CurrentSalary}",
            NewValue = $"Desig: {promotion.NewDesignationId}, Grade: {promotion.NewGradeId}, Salary: {promotion.NewSalary}",
            Reason = promotion.Reason,
            ChangedByUserId = _currentUser.UserId,
            ChangedByName = _currentUser.FullName ?? "HR Admin",
            CreatedAt = DateTime.UtcNow
        });

        _context.AuditLogs.Add(new AuditLog
        {
            TenantId = employee.TenantId,
            UserId = _currentUser.UserId ?? 1,
            Action = "PROMOTE_EMPLOYEE",
            EntityName = "EmployeePromotion",
            EntityId = promotion.Id.ToString(),
            Details = $"Approved promotion for employee #{employee.Id} ({employee.FullName})",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Promotion approved and applied successfully." });
    }

    [HttpPut("promotions/{id:int}/reject")]
    [RequirePermission(HrmsPermissions.ManageEmployees)]
    public async Task<IActionResult> RejectPromotion(int id, [FromBody] string? remarks, CancellationToken cancellationToken)
    {
        var promotion = await _context.EmployeePromotions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (promotion == null) return NotFound();

        promotion.Status = "Rejected";
        if (!string.IsNullOrWhiteSpace(remarks)) promotion.Remarks = remarks.Trim();
        promotion.ApprovedByUserId = _currentUser.UserId;
        promotion.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Promotion request rejected." });
    }

    // =========================================================================
    // 3. SALARY REVISIONS
    // =========================================================================
    [HttpGet("salary-revisions")]
    public async Task<IActionResult> GetSalaryRevisions([FromQuery] int? employeeId, CancellationToken cancellationToken)
    {
        var query = _context.SalaryRevisionRequests.AsNoTracking().Include(s => s.Employee).AsQueryable();
        if (employeeId.HasValue) query = query.Where(s => s.EmployeeId == employeeId.Value);

        var list = await query.OrderByDescending(s => s.CreatedAt).ToListAsync(cancellationToken);
        return Ok(list.Select(s => new SalaryRevisionRequestDto
        {
            Id = s.Id,
            EmployeeId = s.EmployeeId,
            EmployeeName = s.Employee?.FullName ?? string.Empty,
            EmployeeCode = s.Employee?.EmployeeCode ?? string.Empty,
            CurrentSalary = s.CurrentSalary,
            NewSalary = s.NewSalary,
            EffectiveDate = s.EffectiveDate,
            RevisionType = s.RevisionType,
            Reason = s.Reason,
            Remarks = s.Remarks,
            RequestedByUserId = s.RequestedByUserId,
            ApprovedByUserId = s.ApprovedByUserId,
            Status = s.Status,
            CreatedAt = s.CreatedAt
        }));
    }

    [HttpPost("salary-revisions")]
    [RequirePermission(HrmsPermissions.ManageEmployees)]
    public async Task<IActionResult> CreateSalaryRevision([FromBody] SalaryRevisionCreateDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == dto.EmployeeId, cancellationToken);
        if (employee == null) return NotFound("Employee not found.");

        var revision = new SalaryRevisionRequest
        {
            EmployeeId = dto.EmployeeId,
            CurrentSalary = employee.CurrentCtc ?? 0,
            NewSalary = dto.NewSalary,
            EffectiveDate = dto.EffectiveDate,
            RevisionType = dto.RevisionType?.Trim() ?? "Annual Increment",
            Reason = dto.Reason.Trim(),
            Remarks = dto.Remarks?.Trim(),
            RequestedByUserId = _currentUser.UserId,
            Status = "Draft",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SalaryRevisionRequests.Add(revision);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(revision);
    }

    [HttpPut("salary-revisions/{id:int}/approve")]
    [RequirePermission(HrmsPermissions.ManageEmployees)]
    public async Task<IActionResult> ApproveSalaryRevision(int id, CancellationToken cancellationToken)
    {
        var revision = await _context.SalaryRevisionRequests.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (revision == null) return NotFound();

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == revision.EmployeeId, cancellationToken);
        if (employee == null) return NotFound("Employee not found.");

        revision.Status = "Approved";
        revision.ApprovedByUserId = _currentUser.UserId;
        revision.UpdatedAt = DateTime.UtcNow;

        employee.CurrentCtc = revision.NewSalary;
        employee.UpdatedAt = DateTime.UtcNow;

        _context.EmployeeLifecycleHistories.Add(new EmployeeLifecycleHistory
        {
            EmployeeId = employee.Id,
            EventType = "Salary Revision",
            OldValue = $"CTC: {revision.CurrentSalary}",
            NewValue = $"CTC: {revision.NewSalary} ({revision.RevisionType})",
            Reason = revision.Reason,
            ChangedByUserId = _currentUser.UserId,
            ChangedByName = _currentUser.FullName ?? "HR Admin",
            CreatedAt = DateTime.UtcNow
        });

        _context.AuditLogs.Add(new AuditLog
        {
            TenantId = employee.TenantId,
            UserId = _currentUser.UserId ?? 1,
            Action = "SALARY_REVISION",
            EntityName = "SalaryRevisionRequest",
            EntityId = revision.Id.ToString(),
            Details = $"Approved salary revision for employee #{employee.Id} from {revision.CurrentSalary} to {revision.NewSalary}",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Salary revision approved and applied." });
    }

    // =========================================================================
    // 4. EXITS / OFFBOARDING
    // =========================================================================
    [HttpGet("exits")]
    public async Task<IActionResult> GetExits([FromQuery] int? employeeId, CancellationToken cancellationToken)
    {
        var query = _context.EmployeeExits.AsNoTracking()
            .Include(x => x.Employee)
                .ThenInclude(e => e!.Department)
            .Include(x => x.Employee)
                .ThenInclude(e => e!.Designation)
            .AsQueryable();

        if (employeeId.HasValue) query = query.Where(x => x.EmployeeId == employeeId.Value);

        var list = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return Ok(list.Select(x => new EmployeeExitDto
        {
            Id = x.Id,
            EmployeeId = x.EmployeeId,
            EmployeeName = x.Employee?.FullName ?? string.Empty,
            EmployeeCode = x.Employee?.EmployeeCode ?? string.Empty,
            DepartmentName = x.Employee?.Department?.Name ?? string.Empty,
            DesignationName = x.Employee?.Designation?.Name ?? string.Empty,
            ExitType = x.ExitType,
            ResignationDate = x.ResignationDate,
            NoticePeriodDays = x.NoticePeriodDays,
            LastWorkingDate = x.LastWorkingDate,
            Reason = x.Reason,
            Remarks = x.Remarks,
            Status = x.Status,
            AssetClearanceStatus = x.AssetClearanceStatus,
            LeaveSettlementStatus = x.LeaveSettlementStatus,
            PayrollSettlementStatus = x.PayrollSettlementStatus,
            CreatedAt = x.CreatedAt
        }));
    }

    [HttpPost("exits")]
    [RequirePermission(HrmsPermissions.ManageEmployees)]
    public async Task<IActionResult> InitiateExit([FromBody] EmployeeExitCreateDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == dto.EmployeeId, cancellationToken);
        if (employee == null) return NotFound("Employee not found.");

        if (dto.LastWorkingDate < dto.ResignationDate)
        {
            return BadRequest("Last working date cannot be earlier than resignation date.");
        }

        var exit = new EmployeeExit
        {
            EmployeeId = dto.EmployeeId,
            ExitType = dto.ExitType?.Trim() ?? "RESIGNATION",
            ResignationDate = dto.ResignationDate,
            NoticePeriodDays = dto.NoticePeriodDays,
            LastWorkingDate = dto.LastWorkingDate,
            Reason = dto.Reason.Trim(),
            Remarks = dto.Remarks?.Trim(),
            Status = "Under Notice",
            AssetClearanceStatus = "Pending",
            LeaveSettlementStatus = "Pending",
            PayrollSettlementStatus = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Update employee status to On Notice or Resigned
        employee.Status = "On Notice";
        employee.UpdatedAt = DateTime.UtcNow;

        _context.EmployeeExits.Add(exit);

        _context.EmployeeLifecycleHistories.Add(new EmployeeLifecycleHistory
        {
            EmployeeId = employee.Id,
            EventType = "Exit Initiated",
            OldValue = "Active",
            NewValue = $"Exit: {exit.ExitType}, LWD: {exit.LastWorkingDate}",
            Reason = exit.Reason,
            ChangedByUserId = _currentUser.UserId,
            ChangedByName = _currentUser.FullName ?? "HR Admin",
            CreatedAt = DateTime.UtcNow
        });

        _context.AuditLogs.Add(new AuditLog
        {
            TenantId = employee.TenantId,
            UserId = _currentUser.UserId ?? 1,
            Action = "EMPLOYEE_RESIGNATION",
            EntityName = "EmployeeExit",
            EntityId = employee.Id.ToString(),
            Details = $"Initiated exit ({exit.ExitType}) for employee #{employee.Id}",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(exit);
    }

    [HttpPut("exits/{id:int}/clearance")]
    [RequirePermission(HrmsPermissions.ManageEmployees)]
    public async Task<IActionResult> UpdateExitClearance(int id, [FromBody] EmployeeExitClearanceUpdateDto dto, CancellationToken cancellationToken)
    {
        var exit = await _context.EmployeeExits.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (exit == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.AssetClearanceStatus)) exit.AssetClearanceStatus = dto.AssetClearanceStatus.Trim();
        if (!string.IsNullOrWhiteSpace(dto.LeaveSettlementStatus)) exit.LeaveSettlementStatus = dto.LeaveSettlementStatus.Trim();
        if (!string.IsNullOrWhiteSpace(dto.PayrollSettlementStatus)) exit.PayrollSettlementStatus = dto.PayrollSettlementStatus.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Status)) exit.Status = dto.Status.Trim();

        exit.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(exit);
    }

    [HttpPut("exits/{id:int}/complete")]
    [RequirePermission(HrmsPermissions.ManageEmployees)]
    public async Task<IActionResult> CompleteExit(int id, CancellationToken cancellationToken)
    {
        var exit = await _context.EmployeeExits.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (exit == null) return NotFound();

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == exit.EmployeeId, cancellationToken);
        if (employee == null) return NotFound("Employee not found.");

        exit.Status = "Completed";
        exit.UpdatedAt = DateTime.UtcNow;

        employee.Status = "Exited";
        employee.UpdatedAt = DateTime.UtcNow;

        _context.EmployeeLifecycleHistories.Add(new EmployeeLifecycleHistory
        {
            EmployeeId = employee.Id,
            EventType = "Employee Exited",
            OldValue = "On Notice",
            NewValue = "Exited",
            Reason = $"Offboarding completed on {DateTime.UtcNow:yyyy-MM-dd}",
            ChangedByUserId = _currentUser.UserId,
            ChangedByName = _currentUser.FullName ?? "HR Admin",
            CreatedAt = DateTime.UtcNow
        });

        _context.AuditLogs.Add(new AuditLog
        {
            TenantId = employee.TenantId,
            UserId = _currentUser.UserId ?? 1,
            Action = "EMPLOYEE_EXIT",
            EntityName = "EmployeeExit",
            EntityId = exit.Id.ToString(),
            Details = $"Completed exit offboarding for employee #{employee.Id}",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Employee offboarding finalized. Status set to Exited." });
    }

    // =========================================================================
    // 5. LIFECYCLE HISTORY TIMELINE
    // =========================================================================
    [HttpGet("history/{employeeId:int}")]
    public async Task<IActionResult> GetEmployeeHistory(int employeeId, CancellationToken cancellationToken)
    {
        var accessError = this.EnsureEmployeeAccess(_currentUser, employeeId);
        if (accessError != null) return accessError;

        var histories = await _context.EmployeeLifecycleHistories
            .AsNoTracking()
            .Where(h => h.EmployeeId == employeeId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(histories.Select(h => new EmployeeLifecycleHistoryDto
        {
            Id = h.Id,
            EmployeeId = h.EmployeeId,
            EventType = h.EventType,
            OldValue = h.OldValue,
            NewValue = h.NewValue,
            Reason = h.Reason,
            ChangedByUserId = h.ChangedByUserId,
            ChangedByName = h.ChangedByName,
            CreatedAt = h.CreatedAt
        }));
    }
}
