using CRM.DATA;
using CRM.DTO;
using CRM.models;
using CRM.Services;
using Microsoft.EntityFrameworkCore;

namespace CRM.Services
{
    public interface ILeadSyncManagementService
    {
        Task<IReadOnlyList<LeadSyncIntervalOptionDto>> ListIntervalsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<LeadSyncEligibleUserDto>> ListEligibleUsersAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<LeadSyncSourceDto>> ListSourcesForAdminAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<LeadSyncMyAccessDto>> ListMyAccessAsync(int userId, CancellationToken cancellationToken = default);
        Task<bool> IsUserAssignedToSourceAsync(int userId, int sourceId, CancellationToken cancellationToken = default);
        Task UpdateAssignmentsAsync(int sourceId, IReadOnlyList<int> userIds, int actingUserId, CancellationToken cancellationToken = default);
        Task UpdateAutoSyncAsync(int sourceId, LeadSyncUpdateAutoSyncDto dto, int actingUserId, CancellationToken cancellationToken = default);
        Task RecordManualSyncLogAsync(int userId, LeadSyncManualLogDto dto, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<LeadSyncLogDto>> ListLogsAsync(LeadSyncLogQueryDto query, CancellationToken cancellationToken = default);
    }

    public class LeadSyncManagementService : ILeadSyncManagementService
    {
        private readonly TaskDbcontext _db;
        private readonly IRbacService _rbac;

        public LeadSyncManagementService(TaskDbcontext db, IRbacService rbac)
        {
            _db = db;
            _rbac = rbac;
        }

        public async Task<IReadOnlyList<LeadSyncIntervalOptionDto>> ListIntervalsAsync(
            CancellationToken cancellationToken = default)
        {
            return await _db.LeadSyncIntervalOptions.AsNoTracking()
                .Where(o => o.IsActive)
                .OrderBy(o => o.SortOrder)
                .Select(o => new LeadSyncIntervalOptionDto
                {
                    Id = o.Id,
                    Hours = o.Hours,
                    Label = o.Label,
                    SortOrder = o.SortOrder,
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<LeadSyncEligibleUserDto>> ListEligibleUsersAsync(
            CancellationToken cancellationToken = default)
        {
            var leadsViewPermissionId = await _db.Permissions.AsNoTracking()
                .Where(p => p.Code == "leads.view")
                .Select(p => p.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var roleIdsWithLeadsView = await _db.RolePermissions.AsNoTracking()
                .Where(rp => rp.PermissionId == leadsViewPermissionId)
                .Select(rp => rp.RoleId)
                .Distinct()
                .ToListAsync(cancellationToken);

            return await _db.Users.AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.IsActive
                    && u.Role != null
                    && u.Role.IsActive
                    && u.RoleId != null
                    && roleIdsWithLeadsView.Contains(u.RoleId.Value))
                .OrderBy(u => u.FullName)
                .Select(u => new LeadSyncEligibleUserDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    RoleName = u.Role!.Name,
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<LeadSyncSourceDto>> ListSourcesForAdminAsync(
            CancellationToken cancellationToken = default)
        {
            var sources = await _db.LeadSyncSources.AsNoTracking()
                .Where(s => s.IsActive)
                .Include(s => s.Config!)
                    .ThenInclude(c => c!.IntervalOption)
                .Include(s => s.Assignments)
                    .ThenInclude(a => a.User)
                .OrderBy(s => s.SortOrder)
                .ToListAsync(cancellationToken);

            return sources.Select(MapSourceDto).ToList();
        }

        public async Task<IReadOnlyList<LeadSyncMyAccessDto>> ListMyAccessAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            var isAdmin = await _rbac.IsAdminUserAsync(userId);

            var assignedSourceIds = await _db.LeadSyncSourceAssignments.AsNoTracking()
                .Where(a => a.UserId == userId)
                .Select(a => a.SourceId)
                .ToListAsync(cancellationToken);

            if (!isAdmin && assignedSourceIds.Count == 0)
            {
                return Array.Empty<LeadSyncMyAccessDto>();
            }

            IQueryable<LeadSyncSource> q = _db.LeadSyncSources.AsNoTracking()
                .Where(s => s.IsActive)
                .Include(s => s.Config!)
                    .ThenInclude(c => c!.IntervalOption);

            if (!isAdmin)
            {
                q = q.Where(s => assignedSourceIds.Contains(s.Id));
            }
            else if (assignedSourceIds.Count > 0)
            {
                q = q.Where(s => assignedSourceIds.Contains(s.Id));
            }
            else
            {
                return Array.Empty<LeadSyncMyAccessDto>();
            }

            var sources = await q.OrderBy(s => s.SortOrder).ToListAsync(cancellationToken);

            return sources.Select(s => new LeadSyncMyAccessDto
            {
                SourceId = s.Id,
                Code = s.Code,
                DisplayName = s.DisplayName,
                SyncButtonLabel = $"Sync {s.DisplayName}",
                ApiIntegrationReady = s.ApiIntegrationReady,
                AutoSyncEnabled = s.Config?.AutoSyncEnabled ?? false,
                LastSyncAt = s.Config?.LastSyncAt,
                NextSyncAt = s.Config?.NextSyncAt,
            }).ToList();
        }

        public async Task<bool> IsUserAssignedToSourceAsync(
            int userId,
            int sourceId,
            CancellationToken cancellationToken = default)
        {
            if (await _rbac.IsAdminUserAsync(userId))
            {
                return await _db.LeadSyncSourceAssignments.AsNoTracking()
                    .AnyAsync(a => a.UserId == userId && a.SourceId == sourceId, cancellationToken);
            }

            return await _db.LeadSyncSourceAssignments.AsNoTracking()
                .AnyAsync(a => a.UserId == userId && a.SourceId == sourceId, cancellationToken);
        }

        public async Task UpdateAssignmentsAsync(
            int sourceId,
            IReadOnlyList<int> userIds,
            int actingUserId,
            CancellationToken cancellationToken = default)
        {
            var source = await _db.LeadSyncSources
                .FirstOrDefaultAsync(s => s.Id == sourceId && s.IsActive, cancellationToken)
                ?? throw new InvalidOperationException("Source not found.");

            var eligibleIds = (await ListEligibleUsersAsync(cancellationToken))
                .Select(u => u.Id)
                .ToHashSet();

            var distinct = userIds
                .Where(id => id > 0 && eligibleIds.Contains(id))
                .Distinct()
                .ToList();

            var existing = await _db.LeadSyncSourceAssignments
                .Where(a => a.SourceId == sourceId)
                .ToListAsync(cancellationToken);
            _db.LeadSyncSourceAssignments.RemoveRange(existing);

            var now = DateTime.UtcNow;
            for (var i = 0; i < distinct.Count; i++)
            {
                _db.LeadSyncSourceAssignments.Add(new LeadSyncSourceAssignment
                {
                    SourceId = sourceId,
                    UserId = distinct[i],
                    SortOrder = i,
                    CreatedAt = now,
                    CreatedBy = actingUserId,
                });
            }

            source.UpdatedAt = now;
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAutoSyncAsync(
            int sourceId,
            LeadSyncUpdateAutoSyncDto dto,
            int actingUserId,
            CancellationToken cancellationToken = default)
        {
            var config = await _db.LeadSyncSourceConfigs
                .Include(c => c.IntervalOption)
                .FirstOrDefaultAsync(c => c.SourceId == sourceId, cancellationToken)
                ?? throw new InvalidOperationException("Source config not found.");

            config.AutoSyncEnabled = dto.AutoSyncEnabled;
            config.UpdatedBy = actingUserId;
            config.UpdatedAt = DateTime.UtcNow;

            if (dto.AutoSyncEnabled)
            {
                if (dto.IntervalOptionId is not > 0)
                {
                    throw new InvalidOperationException("Interval is required when auto sync is enabled.");
                }

                var interval = await _db.LeadSyncIntervalOptions.AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == dto.IntervalOptionId && o.IsActive, cancellationToken)
                    ?? throw new InvalidOperationException("Invalid interval option.");

                config.IntervalOptionId = interval.Id;
                config.NextSyncAt = DateTime.UtcNow.AddHours(interval.Hours);
            }
            else
            {
                config.IntervalOptionId = dto.IntervalOptionId is > 0 ? dto.IntervalOptionId : config.IntervalOptionId;
                config.NextSyncAt = null;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task RecordManualSyncLogAsync(
            int userId,
            LeadSyncManualLogDto dto,
            CancellationToken cancellationToken = default)
        {
            var assigned = await IsUserAssignedToSourceAsync(userId, dto.SourceId, cancellationToken);
            if (!assigned)
            {
                throw new UnauthorizedAccessException("User is not assigned to this source.");
            }

            var status = dto.FailedCount > 0 && dto.TotalCreated > 0
                ? LeadSyncStatus.Partial
                : dto.FailedCount > 0 || !string.IsNullOrWhiteSpace(dto.ErrorMessage)
                    ? LeadSyncStatus.Failed
                    : LeadSyncStatus.Completed;

            var log = new LeadSyncLog
            {
                SourceId = dto.SourceId,
                SyncType = LeadSyncType.Manual,
                StartedAt = dto.StartedAt.ToUniversalTime(),
                EndedAt = dto.EndedAt.ToUniversalTime(),
                TotalReceived = dto.TotalReceived,
                TotalCreated = dto.TotalCreated,
                FailedCount = dto.FailedCount,
                TriggeredByUserId = userId,
                Status = status,
                ErrorMessage = dto.ErrorMessage,
                CreatedAt = DateTime.UtcNow,
            };
            _db.LeadSyncLogs.Add(log);

            var config = await _db.LeadSyncSourceConfigs
                .Include(c => c.IntervalOption)
                .FirstOrDefaultAsync(c => c.SourceId == dto.SourceId, cancellationToken);
            if (config != null)
            {
                config.LastSyncAt = log.EndedAt ?? log.StartedAt;
                if (config.AutoSyncEnabled && config.IntervalOption != null)
                {
                    config.NextSyncAt = config.LastSyncAt.Value.AddHours(config.IntervalOption.Hours);
                }

                config.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<LeadSyncLogDto>> ListLogsAsync(
            LeadSyncLogQueryDto query,
            CancellationToken cancellationToken = default)
        {
            var limit = Math.Clamp(query.Limit, 1, 200);
            var q = _db.LeadSyncLogs.AsNoTracking()
                .Include(l => l.Source)
                .Include(l => l.TriggeredByUser)
                .AsQueryable();

            if (query.SourceId is > 0)
            {
                q = q.Where(l => l.SourceId == query.SourceId);
            }

            var rows = await q
                .OrderByDescending(l => l.StartedAt)
                .Take(limit)
                .ToListAsync(cancellationToken);

            return rows.Select(l => new LeadSyncLogDto
            {
                Id = l.Id,
                SourceId = l.SourceId,
                SourceName = l.Source?.DisplayName ?? string.Empty,
                SyncType = l.SyncType.ToString(),
                StartedAt = l.StartedAt,
                EndedAt = l.EndedAt,
                TotalReceived = l.TotalReceived,
                TotalCreated = l.TotalCreated,
                FailedCount = l.FailedCount,
                TriggeredByName = l.TriggeredByUser?.FullName,
                Status = l.Status.ToString(),
                ErrorMessage = l.ErrorMessage,
            }).ToList();
        }

        private static LeadSyncSourceDto MapSourceDto(LeadSyncSource s)
        {
            var assignments = s.Assignments
                .Where(a => a.User != null && a.User.IsActive)
                .OrderBy(a => a.SortOrder)
                .Select(a => new LeadSyncAssignmentDto
                {
                    UserId = a.UserId,
                    FullName = a.User!.FullName,
                    Email = a.User.Email,
                    SortOrder = a.SortOrder,
                })
                .ToList();

            return new LeadSyncSourceDto
            {
                Id = s.Id,
                Code = s.Code,
                DisplayName = s.DisplayName,
                MarkerName = s.MarkerName,
                ApiIntegrationReady = s.ApiIntegrationReady,
                AutoSyncEnabled = s.Config?.AutoSyncEnabled ?? false,
                IntervalOptionId = s.Config?.IntervalOptionId,
                IntervalHours = s.Config?.IntervalOption?.Hours,
                IntervalLabel = s.Config?.IntervalOption?.Label,
                LastSyncAt = s.Config?.LastSyncAt,
                NextSyncAt = s.Config?.NextSyncAt,
                Assignments = assignments,
            };
        }
    }
}
