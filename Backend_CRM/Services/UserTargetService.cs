using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Services
{
    public interface IUserTargetService
    {
        Task<IReadOnlyList<UserTargetTypeDto>> ListTargetTypesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<UserTargetSalesUserDto>> ListSalesUsersAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<UserTargetDto>> ListTargetsAsync(
            int? filterUserId,
            bool includeInactive,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<UserTargetDto>> ListMonitorAsync(
            UserTargetMonitorQueryDto query,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<UserTargetWidgetDto>> ListMyWidgetsAsync(
            int userId,
            CancellationToken cancellationToken = default);
        Task<UserTargetDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<UserTargetDto> CreateAsync(UserTargetUpsertDto dto, CancellationToken cancellationToken = default);
        Task<UserTargetDto> UpdateAsync(int id, UserTargetUpsertDto dto, CancellationToken cancellationToken = default);
        Task<UserTargetDto> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);
        Task RecalculateTargetAsync(int targetId, CancellationToken cancellationToken = default);
        Task RecalculateForUserAsync(int userId, CancellationToken cancellationToken = default);
        Task RecalculateForDealAsync(int dealId, int? previousOwnerId = null, CancellationToken cancellationToken = default);
    }

    public class UserTargetService : IUserTargetService
    {
        private readonly TaskDbcontext _db;

        public UserTargetService(TaskDbcontext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<UserTargetTypeDto>> ListTargetTypesAsync(
            CancellationToken cancellationToken = default)
        {
            return await _db.UserTargetTypes.AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.Name)
                .Select(t => new UserTargetTypeDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    SortOrder = t.SortOrder,
                    IsActive = t.IsActive,
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<UserTargetSalesUserDto>> ListSalesUsersAsync(
            CancellationToken cancellationToken = default)
        {
            return await _db.Users.AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.IsActive
                    && u.Role != null
                    && u.Role.IsActive
                    && u.Role.Name.ToLower() == "sales")
                .OrderBy(u => u.FullName)
                .Select(u => new UserTargetSalesUserDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    RoleName = u.Role!.Name,
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<UserTargetDto>> ListTargetsAsync(
            int? filterUserId,
            bool includeInactive,
            CancellationToken cancellationToken = default)
        {
            var q = BuildTargetQuery(includeInactive);
            if (filterUserId is > 0)
            {
                q = q.Where(t => t.UserId == filterUserId);
            }

            var rows = await q
                .OrderByDescending(t => t.IsActive)
                .ThenByDescending(t => t.StartDate)
                .ToListAsync(cancellationToken);

            return rows.Select(MapToDto).ToList();
        }

        public async Task<IReadOnlyList<UserTargetDto>> ListMonitorAsync(
            UserTargetMonitorQueryDto query,
            CancellationToken cancellationToken = default)
        {
            var q = BuildTargetQuery(includeInactive: true);

            if (query.UserId is > 0)
            {
                q = q.Where(t => t.UserId == query.UserId);
            }

            if (query.TargetTypeId is > 0)
            {
                q = q.Where(t => t.TargetTypeId == query.TargetTypeId);
            }

            if (query.IsActive.HasValue)
            {
                q = q.Where(t => t.IsActive == query.IsActive.Value);
            }

            var search = query.Search?.Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lower = search.ToLower();
                q = q.Where(t =>
                    t.User!.FullName.ToLower().Contains(lower)
                    || t.User.Email.ToLower().Contains(lower)
                    || t.TargetType!.Name.ToLower().Contains(lower));
            }

            var sortBy = (query.SortBy ?? "userName").Trim().ToLowerInvariant();
            var desc = string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

            q = sortBy switch
            {
                "targettype" or "targettypename" => desc
                    ? q.OrderByDescending(t => t.TargetType!.Name)
                    : q.OrderBy(t => t.TargetType!.Name),
                "targetamount" => desc
                    ? q.OrderByDescending(t => t.TargetAmount)
                    : q.OrderBy(t => t.TargetAmount),
                "achievedamount" => desc
                    ? q.OrderByDescending(t => t.AchievedAmount)
                    : q.OrderBy(t => t.AchievedAmount),
                "achievementpercent" => desc
                    ? q.OrderByDescending(t => t.TargetAmount == 0 ? 0 : t.AchievedAmount / t.TargetAmount)
                    : q.OrderBy(t => t.TargetAmount == 0 ? 0 : t.AchievedAmount / t.TargetAmount),
                "status" or "isactive" => desc
                    ? q.OrderByDescending(t => t.IsActive)
                    : q.OrderBy(t => t.IsActive),
                "startdate" => desc
                    ? q.OrderByDescending(t => t.StartDate)
                    : q.OrderBy(t => t.StartDate),
                _ => desc
                    ? q.OrderByDescending(t => t.User!.FullName)
                    : q.OrderBy(t => t.User!.FullName),
            };

            var rows = await q.ToListAsync(cancellationToken);
            return rows.Select(MapToDto).ToList();
        }

        public async Task<IReadOnlyList<UserTargetWidgetDto>> ListMyWidgetsAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var rows = await _db.UserTargets.AsNoTracking()
                .Include(t => t.TargetType)
                .Where(t => t.UserId == userId
                    && t.IsActive
                    && t.StartDate <= today
                    && t.EndDate >= today)
                .OrderBy(t => t.TargetType!.SortOrder)
                .ThenBy(t => t.StartDate)
                .ToListAsync(cancellationToken);

            return rows.Select(t =>
            {
                var remaining = Math.Max(0m, t.TargetAmount - t.AchievedAmount);
                var pct = t.TargetAmount <= 0
                    ? 0m
                    : Math.Min(100m, Math.Round(t.AchievedAmount / t.TargetAmount * 100m, 1));

                return new UserTargetWidgetDto
                {
                    TargetId = t.Id,
                    TargetTypeName = t.TargetType?.Name ?? string.Empty,
                    TargetAmount = t.TargetAmount,
                    AchievedAmount = t.AchievedAmount,
                    RemainingAmount = remaining,
                    AchievementPercent = pct,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    IsActive = t.IsActive,
                };
            }).ToList();
        }

        public async Task<UserTargetDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var row = await BuildTargetQuery(includeInactive: true)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
            return row == null ? null : MapToDto(row);
        }

        public async Task<UserTargetDto> CreateAsync(
            UserTargetUpsertDto dto,
            CancellationToken cancellationToken = default)
        {
            await ValidateUpsertAsync(dto, null, cancellationToken);

            var entity = new UserTarget
            {
                UserId = dto.UserId,
                TargetTypeId = dto.TargetTypeId,
                TargetAmount = dto.TargetAmount,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsActive = dto.IsActive,
            };

            await _db.UserTargets.AddAsync(entity, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await RecalculateTargetAsync(entity.Id, cancellationToken);

            return (await GetByIdAsync(entity.Id, cancellationToken))!;
        }

        public async Task<UserTargetDto> UpdateAsync(
            int id,
            UserTargetUpsertDto dto,
            CancellationToken cancellationToken = default)
        {
            var entity = await _db.UserTargets.FindAsync([id], cancellationToken);
            if (entity == null)
            {
                throw new InvalidOperationException("Target not found.");
            }

            await ValidateUpsertAsync(dto, id, cancellationToken);

            entity.UserId = dto.UserId;
            entity.TargetTypeId = dto.TargetTypeId;
            entity.TargetAmount = dto.TargetAmount;
            entity.StartDate = dto.StartDate;
            entity.EndDate = dto.EndDate;
            entity.IsActive = dto.IsActive;

            await _db.SaveChangesAsync(cancellationToken);
            await RecalculateTargetAsync(id, cancellationToken);

            return (await GetByIdAsync(id, cancellationToken))!;
        }

        public async Task<UserTargetDto> SetActiveAsync(
            int id,
            bool isActive,
            CancellationToken cancellationToken = default)
        {
            var entity = await _db.UserTargets.FindAsync([id], cancellationToken);
            if (entity == null)
            {
                throw new InvalidOperationException("Target not found.");
            }

            entity.IsActive = isActive;
            await _db.SaveChangesAsync(cancellationToken);
            await RecalculateTargetAsync(id, cancellationToken);

            return (await GetByIdAsync(id, cancellationToken))!;
        }

        public async Task RecalculateTargetAsync(int targetId, CancellationToken cancellationToken = default)
        {
            var target = await _db.UserTargets.FirstOrDefaultAsync(t => t.Id == targetId, cancellationToken);
            if (target == null)
            {
                return;
            }

            target.AchievedAmount = await UserTargetAchievementHelper.CalculateAchievedAmountAsync(
                _db,
                target.UserId,
                target.StartDate,
                target.EndDate,
                cancellationToken);
            target.AchievedCalculatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task RecalculateForUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            var targetIds = await _db.UserTargets
                .Where(t => t.UserId == userId)
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);

            foreach (var targetId in targetIds)
            {
                await RecalculateTargetAsync(targetId, cancellationToken);
            }
        }

        public async Task RecalculateForDealAsync(
            int dealId,
            int? previousOwnerId = null,
            CancellationToken cancellationToken = default)
        {
            var deal = await _db.Deals.AsNoTracking()
                .Where(d => d.Id == dealId)
                .Select(d => new { d.DealOwnerId })
                .FirstOrDefaultAsync(cancellationToken);

            var userIds = new HashSet<int>();
            if (previousOwnerId is > 0)
            {
                userIds.Add(previousOwnerId.Value);
            }

            if (deal?.DealOwnerId is > 0)
            {
                userIds.Add(deal.DealOwnerId.Value);
            }

            foreach (var userId in userIds)
            {
                await RecalculateForUserAsync(userId, cancellationToken);
            }
        }

        private IQueryable<UserTarget> BuildTargetQuery(bool includeInactive)
        {
            var q = _db.UserTargets.AsNoTracking()
                .Include(t => t.User)
                .Include(t => t.TargetType)
                .AsQueryable();

            if (!includeInactive)
            {
                q = q.Where(t => t.IsActive);
            }

            return q;
        }

        private static UserTargetDto MapToDto(UserTarget t)
        {
            var remaining = Math.Max(0m, t.TargetAmount - t.AchievedAmount);
            var pct = t.TargetAmount <= 0
                ? 0m
                : Math.Min(100m, Math.Round(t.AchievedAmount / t.TargetAmount * 100m, 1));

            return new UserTargetDto
            {
                Id = t.Id,
                UserId = t.UserId,
                UserName = t.User?.FullName ?? string.Empty,
                UserEmail = t.User?.Email ?? string.Empty,
                TargetTypeId = t.TargetTypeId,
                TargetTypeName = t.TargetType?.Name ?? string.Empty,
                TargetAmount = t.TargetAmount,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                IsActive = t.IsActive,
                AchievedAmount = t.AchievedAmount,
                RemainingAmount = remaining,
                AchievementPercent = pct,
                AchievedCalculatedAt = t.AchievedCalculatedAt,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
            };
        }

        private async Task ValidateUpsertAsync(
            UserTargetUpsertDto dto,
            int? existingId,
            CancellationToken cancellationToken)
        {
            if (dto.UserId <= 0)
            {
                throw new InvalidOperationException("Sales user is required.");
            }

            if (dto.TargetTypeId <= 0)
            {
                throw new InvalidOperationException("Target type is required.");
            }

            if (dto.TargetAmount < 0)
            {
                throw new InvalidOperationException("Target amount cannot be negative.");
            }

            if (dto.EndDate < dto.StartDate)
            {
                throw new InvalidOperationException("End date must be on or after start date.");
            }

            var isSalesUser = await _db.Users.AsNoTracking()
                .Include(u => u.Role)
                .AnyAsync(u => u.Id == dto.UserId
                    && u.IsActive
                    && u.Role != null
                    && u.Role.IsActive
                    && u.Role.Name.ToLower() == "sales",
                    cancellationToken);

            if (!isSalesUser)
            {
                throw new InvalidOperationException("Target can only be assigned to an active Sales user.");
            }

            var typeExists = await _db.UserTargetTypes.AsNoTracking()
                .AnyAsync(t => t.Id == dto.TargetTypeId && t.IsActive, cancellationToken);

            if (!typeExists)
            {
                throw new InvalidOperationException("Invalid or inactive target type.");
            }
        }
    }
}
