using HRMS.Authorization;
using HRMS.Data;
using HRMS.DTOs;
using HRMS.Interfaces;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Services;

public sealed class LeaveService : ILeaveService
{
    private readonly HRMSDbContext _context;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LeaveService> _logger;

    public LeaveService(
        HRMSDbContext context,
        ICurrentUserAccessor currentUser,
        IWebHostEnvironment environment,
        ILogger<LeaveService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _environment = environment;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LeaveRequestResponseDto>> GetRequestsAsync(
        int? employeeId,
        int? departmentId,
        int? leaveTypeId,
        string? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        var query = BuildRequestQuery();

        if (_currentUser.IsAdmin)
        {
            if (!_currentUser.HasPermission(HrmsPermissions.LeaveViewAll))
            {
                return Array.Empty<LeaveRequestResponseDto>();
            }

            if (employeeId.HasValue)
            {
                query = query.Where(x => x.EmployeeId == employeeId.Value);
            }

            if (departmentId.HasValue)
            {
                query = query.Where(x => x.Employee != null && x.Employee.DepartmentId == departmentId.Value);
            }
        }
        else
        {
            if (!_currentUser.EmployeeId.HasValue)
            {
                return Array.Empty<LeaveRequestResponseDto>();
            }

            query = query.Where(x => x.EmployeeId == _currentUser.EmployeeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status.Trim());
        }

        if (leaveTypeId.HasValue)
        {
            query = query.Where(x => x.LeaveTypeId == leaveTypeId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.EndDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.StartDate <= toDate.Value);
        }

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return items.Select(MapToResponse).ToList();
    }

    public async Task<LeaveRequestResponseDto?> GetRequestByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await BuildRequestQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (item == null)
        {
            return null;
        }

        if (!CanAccessEmployee(item.EmployeeId))
        {
            return null;
        }

        return MapToResponse(item);
    }

    public async Task<(LeaveRequestResponseDto? Result, string? Error, int StatusCode)> ApplyLeaveAsync(
        LeaveApplyDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAdmin)
        {
            if (!_currentUser.EmployeeId.HasValue || dto.EmployeeId != _currentUser.EmployeeId.Value)
            {
                return (null, "You can only apply leave for your own employee profile.", StatusCodes.Status403Forbidden);
            }

            if (!_currentUser.HasPermission(HrmsPermissions.LeaveApply))
            {
                return (null, "You do not have permission to apply for leave.", StatusCodes.Status403Forbidden);
            }
        }
        else if (!_currentUser.HasPermission(HrmsPermissions.LeaveManage))
        {
            return (null, "You do not have permission to create leave on behalf of employees.", StatusCodes.Status403Forbidden);
        }

        var validationError = await ValidateLeaveApplicationAsync(dto, null, cancellationToken);
        if (validationError != null)
        {
            return (null, validationError, StatusCodes.Status400BadRequest);
        }

        var entity = new LeaveRequest
        {
            EmployeeId = dto.EmployeeId,
            LeaveTypeId = dto.LeaveTypeId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            TotalDays = dto.TotalDays,
            Reason = dto.Reason.Trim(),
            Status = LeaveStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            _context.LeaveRequests.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            try
            {
                await NotifyEmployeeAsync(
                    entity.EmployeeId,
                    entity.Id,
                    "Leave application submitted",
                    $"Your leave request from {entity.StartDate:yyyy-MM-dd} to {entity.EndDate:yyyy-MM-dd} has been submitted and is pending approval.",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Leave request {LeaveRequestId} saved but notification failed.", entity.Id);
            }

            return (await GetRequestByIdAsync(entity.Id, cancellationToken), null, StatusCodes.Status200OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply leave for employee {EmployeeId}.", dto.EmployeeId);
            return (null, "Failed to save leave request. Ensure database migrations are applied.", StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<(LeaveRequestResponseDto? Result, string? Error, int StatusCode)> ApproveLeaveAsync(
        int id,
        LeaveActionDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.HasPermission(HrmsPermissions.LeaveApprove))
        {
            return (null, "You do not have permission to approve leave requests.", StatusCodes.Status403Forbidden);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var entity = await _context.LeaveRequests
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
        {
            return (null, "Leave request not found.", StatusCodes.Status404NotFound);
        }

        if (!string.Equals(entity.Status, LeaveStatus.Pending, StringComparison.OrdinalIgnoreCase))
        {
            return (null, "Only pending leave requests can be approved.", StatusCodes.Status400BadRequest);
        }

        var year = entity.StartDate.Year;
        var allocation = await EnsureAllocationAsync(entity.EmployeeId, entity.LeaveTypeId, year, cancellationToken);
        var available = await GetAvailableDaysAsync(entity.EmployeeId, entity.LeaveTypeId, year, entity.Id, cancellationToken);

        if (entity.TotalDays > available)
        {
            return (null, $"Insufficient leave balance. Available: {available} day(s), requested: {entity.TotalDays} day(s).", StatusCodes.Status400BadRequest);
        }

        entity.Status = LeaveStatus.Approved;
        entity.ApprovalRemarks = dto.Remarks?.Trim();
        entity.ApprovedByUserId = _currentUser.UserId;
        entity.ApprovedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        allocation.UsedDays += entity.TotalDays;

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await NotifyEmployeeAsync(
            entity.EmployeeId,
            entity.Id,
            "Leave request approved",
            $"Your leave request from {entity.StartDate:yyyy-MM-dd} to {entity.EndDate:yyyy-MM-dd} has been approved."
            + (string.IsNullOrWhiteSpace(entity.ApprovalRemarks) ? string.Empty : $" Remarks: {entity.ApprovalRemarks}"),
            cancellationToken);

        return (await GetRequestByIdAsync(entity.Id, cancellationToken), null, StatusCodes.Status200OK);
    }

    public async Task<(LeaveRequestResponseDto? Result, string? Error, int StatusCode)> RejectLeaveAsync(
        int id,
        LeaveActionDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.HasPermission(HrmsPermissions.LeaveApprove))
        {
            return (null, "You do not have permission to reject leave requests.", StatusCodes.Status403Forbidden);
        }

        var entity = await _context.LeaveRequests.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return (null, "Leave request not found.", StatusCodes.Status404NotFound);
        }

        if (!string.Equals(entity.Status, LeaveStatus.Pending, StringComparison.OrdinalIgnoreCase))
        {
            return (null, "Only pending leave requests can be rejected.", StatusCodes.Status400BadRequest);
        }

        if (string.IsNullOrWhiteSpace(dto.Remarks))
        {
            return (null, "Rejection remarks are required.", StatusCodes.Status400BadRequest);
        }

        entity.Status = LeaveStatus.Rejected;
        entity.ApprovalRemarks = dto.Remarks.Trim();
        entity.ApprovedByUserId = _currentUser.UserId;
        entity.ApprovedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await NotifyEmployeeAsync(
            entity.EmployeeId,
            entity.Id,
            "Leave request rejected",
            $"Your leave request from {entity.StartDate:yyyy-MM-dd} to {entity.EndDate:yyyy-MM-dd} has been rejected. Reason: {entity.ApprovalRemarks}",
            cancellationToken);

        return (await GetRequestByIdAsync(entity.Id, cancellationToken), null, StatusCodes.Status200OK);
    }

    public async Task<(LeaveRequestResponseDto? Result, string? Error, int StatusCode)> CancelLeaveAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.LeaveRequests.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return (null, "Leave request not found.", StatusCodes.Status404NotFound);
        }

        if (!_currentUser.IsAdmin)
        {
            if (!_currentUser.EmployeeId.HasValue || entity.EmployeeId != _currentUser.EmployeeId.Value)
            {
                return (null, "You can only cancel your own leave requests.", StatusCodes.Status403Forbidden);
            }

            if (!_currentUser.HasPermission(HrmsPermissions.LeaveApply))
            {
                return (null, "You do not have permission to cancel leave requests.", StatusCodes.Status403Forbidden);
            }
        }
        else if (!_currentUser.HasPermission(HrmsPermissions.LeaveManage))
        {
            return (null, "You do not have permission to cancel leave requests.", StatusCodes.Status403Forbidden);
        }

        if (!string.Equals(entity.Status, LeaveStatus.Pending, StringComparison.OrdinalIgnoreCase))
        {
            return (null, "Only pending leave requests can be cancelled.", StatusCodes.Status400BadRequest);
        }

        entity.Status = LeaveStatus.Cancelled;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetRequestByIdAsync(entity.Id, cancellationToken), null, StatusCodes.Status200OK);
    }

    public async Task<IReadOnlyList<LeaveBalanceItemDto>> GetBalancesAsync(
        int? employeeId,
        int? year,
        CancellationToken cancellationToken = default)
    {
        var targetEmployeeId = ResolveEmployeeId(employeeId);
        if (!targetEmployeeId.HasValue)
        {
            return Array.Empty<LeaveBalanceItemDto>();
        }

        if (!_currentUser.IsAdmin && _currentUser.EmployeeId != targetEmployeeId)
        {
            return Array.Empty<LeaveBalanceItemDto>();
        }

        if (_currentUser.IsAdmin && !_currentUser.HasPermission(HrmsPermissions.LeaveViewAll))
        {
            return Array.Empty<LeaveBalanceItemDto>();
        }

        if (!_currentUser.IsAdmin && !_currentUser.HasPermission(HrmsPermissions.LeaveViewOwn))
        {
            return Array.Empty<LeaveBalanceItemDto>();
        }

        var targetYear = year ?? DateTime.UtcNow.Year;
        var leaveTypes = await _context.LeaveTypes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var allocations = await _context.LeaveAllocations
            .AsNoTracking()
            .Where(x => x.EmployeeId == targetEmployeeId.Value && x.Year == targetYear)
            .ToListAsync(cancellationToken);

        var allocationByType = allocations.ToDictionary(x => x.LeaveTypeId);
        var result = new List<LeaveBalanceItemDto>();

        foreach (var leaveType in leaveTypes)
        {
            if (!allocationByType.TryGetValue(leaveType.Id, out var allocation))
            {
                allocation = await EnsureAllocationAsync(targetEmployeeId.Value, leaveType.Id, targetYear, cancellationToken);
            }

            var pendingDays = await _context.LeaveRequests
                .AsNoTracking()
                .Where(x => x.EmployeeId == targetEmployeeId.Value
                    && x.LeaveTypeId == leaveType.Id
                    && x.Status == LeaveStatus.Pending
                    && x.StartDate.Year == targetYear)
                .SumAsync(x => x.TotalDays, cancellationToken);

            var available = Math.Max(0, allocation.AllocatedDays - allocation.UsedDays - pendingDays);

            result.Add(new LeaveBalanceItemDto
            {
                LeaveTypeId = leaveType.Id,
                LeaveTypeName = leaveType.Name,
                LeaveTypeCode = leaveType.Code,
                AllocatedDays = allocation.AllocatedDays,
                UsedDays = allocation.UsedDays,
                PendingDays = pendingDays,
                AvailableDays = available,
                Year = targetYear
            });
        }

        return result;
    }

    public async Task<(LeaveRequestResponseDto? Result, string? Error, int StatusCode)> SaveAttachmentAsync(
        int id,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.LeaveRequests.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return (null, "Leave request not found.", StatusCodes.Status404NotFound);
        }

        if (!CanAccessEmployee(entity.EmployeeId))
        {
            return (null, "You do not have access to this leave request.", StatusCodes.Status403Forbidden);
        }

        if (file.Length <= 0 || file.Length > 10 * 1024 * 1024)
        {
            return (null, "Attachment must be between 1 byte and 10 MB.", StatusCodes.Status400BadRequest);
        }

        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf", ".png", ".jpg", ".jpeg" };
        var extension = Path.GetExtension(file.FileName);
        if (!allowedExtensions.Contains(extension))
        {
            return (null, "Allowed attachment formats: PDF, PNG, JPG.", StatusCodes.Status400BadRequest);
        }

        var uploadsRoot = Path.Combine(_environment.ContentRootPath, "Uploads", "leave");
        Directory.CreateDirectory(uploadsRoot);

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(uploadsRoot, storedFileName);

        await using (var stream = File.Create(physicalPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        entity.AttachmentPath = Path.Combine("Uploads", "leave", storedFileName).Replace('\\', '/');
        entity.AttachmentFileName = Path.GetFileName(file.FileName);
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetRequestByIdAsync(entity.Id, cancellationToken), null, StatusCodes.Status200OK);
    }

    public async Task<IReadOnlyList<LeaveNotificationDto>> GetNotificationsAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.EmployeeId.HasValue)
        {
            return Array.Empty<LeaveNotificationDto>();
        }

        return await _context.LeaveNotifications
            .AsNoTracking()
            .Where(x => x.EmployeeId == _currentUser.EmployeeId.Value)
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .Select(x => new LeaveNotificationDto
            {
                Id = x.Id,
                LeaveRequestId = x.LeaveRequestId,
                Title = x.Title,
                Message = x.Message,
                IsRead = x.IsRead,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> MarkNotificationReadAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.EmployeeId.HasValue)
        {
            return false;
        }

        var notification = await _context.LeaveNotifications
            .FirstOrDefaultAsync(x => x.Id == id && x.EmployeeId == _currentUser.EmployeeId.Value, cancellationToken);

        if (notification == null)
        {
            return false;
        }

        notification.IsRead = true;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<LeaveRequest> BuildRequestQuery()
    {
        return _context.LeaveRequests
            .AsNoTracking()
            .Include(x => x.Employee!)
                .ThenInclude(e => e!.Department)
            .Include(x => x.LeaveType)
            .Include(x => x.ApprovedByUser);
    }

    private async Task<string?> ValidateLeaveApplicationAsync(
        LeaveApplyDto dto,
        int? excludeRequestId,
        CancellationToken cancellationToken)
    {
        if (dto.EndDate < dto.StartDate)
        {
            return "End date cannot be earlier than start date.";
        }

        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            return "Reason is required.";
        }

        var expectedDays = dto.EndDate.DayNumber - dto.StartDate.DayNumber + 1;
        if (dto.TotalDays != expectedDays)
        {
            return $"Total days must match the selected date range ({expectedDays} day(s)).";
        }

        if (!await _context.Employees.AnyAsync(x => x.Id == dto.EmployeeId, cancellationToken))
        {
            return "Employee not found.";
        }

        var leaveType = await _context.LeaveTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dto.LeaveTypeId && x.IsActive, cancellationToken);

        if (leaveType == null)
        {
            return "Leave type not found or inactive.";
        }

        var overlapQuery = _context.LeaveRequests
            .AsNoTracking()
            .Where(x => x.EmployeeId == dto.EmployeeId
                && LeaveStatus.Active.Contains(x.Status)
                && x.StartDate <= dto.EndDate
                && x.EndDate >= dto.StartDate);

        if (excludeRequestId.HasValue)
        {
            overlapQuery = overlapQuery.Where(x => x.Id != excludeRequestId.Value);
        }

        if (await overlapQuery.AnyAsync(cancellationToken))
        {
            return "Leave dates overlap with an existing pending or approved request.";
        }

        var year = dto.StartDate.Year;
        await EnsureAllocationAsync(dto.EmployeeId, dto.LeaveTypeId, year, cancellationToken);
        var available = await GetAvailableDaysAsync(dto.EmployeeId, dto.LeaveTypeId, year, excludeRequestId, cancellationToken);

        if (dto.TotalDays > available)
        {
            return $"Insufficient leave balance. Available: {available} day(s), requested: {dto.TotalDays} day(s).";
        }

        return null;
    }

    private async Task<int> GetAvailableDaysAsync(
        int employeeId,
        int leaveTypeId,
        int year,
        int? excludeRequestId,
        CancellationToken cancellationToken)
    {
        var allocation = await EnsureAllocationAsync(employeeId, leaveTypeId, year, cancellationToken);

        var pendingQuery = _context.LeaveRequests
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId
                && x.LeaveTypeId == leaveTypeId
                && x.Status == LeaveStatus.Pending
                && x.StartDate.Year == year);

        if (excludeRequestId.HasValue)
        {
            pendingQuery = pendingQuery.Where(x => x.Id != excludeRequestId.Value);
        }

        var pendingDays = await pendingQuery.SumAsync(x => x.TotalDays, cancellationToken);
        return Math.Max(0, allocation.AllocatedDays - allocation.UsedDays - pendingDays);
    }

    private async Task<LeaveAllocation> EnsureAllocationAsync(
        int employeeId,
        int leaveTypeId,
        int year,
        CancellationToken cancellationToken)
    {
        var allocation = await _context.LeaveAllocations
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.LeaveTypeId == leaveTypeId && x.Year == year, cancellationToken);

        if (allocation != null)
        {
            return allocation;
        }

        var leaveType = await _context.LeaveTypes
            .AsNoTracking()
            .FirstAsync(x => x.Id == leaveTypeId, cancellationToken);

        allocation = new LeaveAllocation
        {
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            Year = year,
            AllocatedDays = leaveType.DefaultAllocatedDays,
            UsedDays = 0
        };

        _context.LeaveAllocations.Add(allocation);
        await _context.SaveChangesAsync(cancellationToken);
        return allocation;
    }

    private async Task NotifyEmployeeAsync(
        int employeeId,
        int leaveRequestId,
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        _context.LeaveNotifications.Add(new LeaveNotification
        {
            EmployeeId = employeeId,
            LeaveRequestId = leaveRequestId,
            Title = title,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Leave notification for employee {EmployeeId}: {Title}", employeeId, title);
    }

    private int? ResolveEmployeeId(int? employeeId)
    {
        if (_currentUser.IsAdmin)
        {
            return employeeId ?? _currentUser.EmployeeId;
        }

        return _currentUser.EmployeeId;
    }

    private bool CanAccessEmployee(int employeeId) =>
        _currentUser.IsAdmin || (_currentUser.EmployeeId.HasValue && _currentUser.EmployeeId.Value == employeeId);

    private static LeaveRequestResponseDto MapToResponse(LeaveRequest item) =>
        new()
        {
            Id = item.Id,
            EmployeeId = item.EmployeeId,
            EmployeeName = item.Employee?.FullName,
            EmployeeCode = item.Employee?.EmployeeCode,
            Department = item.Employee?.Department?.Name,
            LeaveTypeId = item.LeaveTypeId,
            LeaveTypeName = item.LeaveType?.Name,
            StartDate = item.StartDate,
            EndDate = item.EndDate,
            TotalDays = item.TotalDays,
            Reason = item.Reason,
            Status = item.Status,
            AttachmentPath = item.AttachmentPath,
            AttachmentFileName = item.AttachmentFileName,
            ApprovalRemarks = item.ApprovalRemarks,
            ApprovedByUserId = item.ApprovedByUserId,
            ApprovedByName = item.ApprovedByUser?.FullName,
            ApprovedAt = item.ApprovedAt,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
}
