using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class DispatchStatusService : IDispatchStatusService
    {
        private readonly ERPDbContext _db;

        public DispatchStatusService(ERPDbContext db)
        {
            _db = db;
        }

        public async Task<List<DispatchStatusListItemDto>> GetDispatchStatusesAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            await EnsureSyncWithPlansAsync(cancellationToken);

            var q = _db.DispatchStatusTracks.AsNoTracking().AsQueryable();

            if (query != null)
            {
                if (!string.IsNullOrWhiteSpace(query.Search))
                {
                    var s = query.Search.Trim().ToLower();
                    q = q.Where(x => x.DispatchNumber.ToLower().Contains(s));
                }

                if (!string.IsNullOrWhiteSpace(query.Status))
                {
                    var statusStr = query.Status.Trim();
                    if (string.Equals(statusStr, "In Transit", StringComparison.OrdinalIgnoreCase))
                    {
                        q = q.Where(x => x.CurrentStatus == DispatchTrackStatus.InTransit);
                    }
                    else if (Enum.TryParse<DispatchTrackStatus>(statusStr.Replace(" ", ""), true, out var st))
                    {
                        q = q.Where(x => x.CurrentStatus == st);
                    }
                }

                if (query.DispatchId.HasValue && query.DispatchId.Value > 0)
                {
                    q = q.Where(x => x.DispatchId == query.DispatchId.Value);
                }

                if (!string.IsNullOrWhiteSpace(query.DateFrom) && DateTime.TryParse(query.DateFrom, out var df))
                {
                    q = q.Where(x => x.DispatchDate >= DateTime.SpecifyKind(df.Date, DateTimeKind.Utc));
                }

                if (!string.IsNullOrWhiteSpace(query.DateTo) && DateTime.TryParse(query.DateTo, out var dt))
                {
                    q = q.Where(x => x.DispatchDate <= DateTime.SpecifyKind(dt.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc));
                }
            }

            var list = await q.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

            return list.Select(x => new DispatchStatusListItemDto
            {
                Id = x.Id,
                DispatchId = x.DispatchId,
                DispatchNumber = x.DispatchNumber,
                CurrentStatus = x.CurrentStatus,
                WarehouseStatus = x.WarehouseStatus,
                VehicleStatus = x.VehicleStatus,
                TransportStatus = x.TransportStatus,
                DispatchDate = x.DispatchDate.ToString("yyyy-MM-dd"),
                ExpectedDelivery = x.ExpectedDelivery.ToString("yyyy-MM-dd"),
                ActualDelivery = x.ActualDelivery?.ToString("yyyy-MM-dd"),
                DelayHours = x.DelayHours
            }).ToList();
        }

        public async Task<DispatchStatusDto> GetDispatchStatusByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var status = await _db.DispatchStatusTracks
                .AsNoTracking()
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (status == null)
                throw new KeyNotFoundException($"Dispatch status with ID {id} not found.");

            return MapToDto(status);
        }

        public async Task<DispatchStatusDto> UpdateDispatchStatusAsync(int id, DispatchStatusUpdateRequestDto payload, string currentUser, CancellationToken cancellationToken = default)
        {
            var status = await _db.DispatchStatusTracks
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (status == null)
                throw new KeyNotFoundException($"Dispatch status with ID {id} not found.");

            var user = string.IsNullOrWhiteSpace(currentUser) ? "System" : currentUser;
            var now = DateTime.UtcNow;
            var prevStatus = status.CurrentStatus;
            var nextStatus = payload.CurrentStatus;

            status.CurrentStatus = nextStatus;
            if (payload.WarehouseStatus.HasValue)
                status.WarehouseStatus = payload.WarehouseStatus.Value;
            if (payload.VehicleStatus.HasValue)
                status.VehicleStatus = payload.VehicleStatus.Value;
            if (payload.TransportStatus.HasValue)
                status.TransportStatus = payload.TransportStatus.Value;

            if (payload.ActualDelivery != null)
            {
                if (string.IsNullOrWhiteSpace(payload.ActualDelivery))
                {
                    status.ActualDelivery = null;
                }
                else if (DateTime.TryParse(payload.ActualDelivery, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var ad))
                {
                    status.ActualDelivery = DateTime.SpecifyKind(ad, DateTimeKind.Utc);
                }
            }

            if (payload.DelayHours.HasValue)
            {
                status.DelayHours = payload.DelayHours.Value;
            }

            if (payload.Remarks != null)
                status.Remarks = payload.Remarks.Trim();
            if (payload.Notes != null)
                status.Notes = payload.Notes.Trim();

            status.UpdatedBy = user;
            status.UpdatedAt = now;

            status.Timeline.Add(new DispatchStatusTimelineEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Date = now,
                User = user,
                Action = "Status updated",
                FromStatus = prevStatus == DispatchTrackStatus.InTransit ? "In Transit" : prevStatus.ToString(),
                ToStatus = nextStatus == DispatchTrackStatus.InTransit ? "In Transit" : nextStatus.ToString(),
                Remarks = payload.Remarks?.Trim()
            });

            await _db.SaveChangesAsync(cancellationToken);
            return MapToDto(status);
        }

        public async Task<DispatchStatusDashboardDto> GetDispatchStatusDashboardAsync(CancellationToken cancellationToken = default)
        {
            await EnsureSyncWithPlansAsync(cancellationToken);

            var ready = await _db.DispatchStatusTracks
                .CountAsync(x => x.CurrentStatus == DispatchTrackStatus.Ready, cancellationToken);

            var inTransit = await _db.DispatchStatusTracks
                .CountAsync(x => x.CurrentStatus == DispatchTrackStatus.InTransit || x.CurrentStatus == DispatchTrackStatus.Dispatched, cancellationToken);

            var delivered = await _db.DispatchStatusTracks
                .CountAsync(x => x.CurrentStatus == DispatchTrackStatus.Delivered || x.CurrentStatus == DispatchTrackStatus.Completed, cancellationToken);

            var delayed = await _db.DispatchStatusTracks
                .CountAsync(x => x.CurrentStatus == DispatchTrackStatus.Delayed, cancellationToken);

            return new DispatchStatusDashboardDto
            {
                Ready = ready,
                InTransit = inTransit,
                Delivered = delivered,
                Delayed = delayed
            };
        }

        private async Task EnsureSyncWithPlansAsync(CancellationToken cancellationToken)
        {
            var existingDispatchIds = await _db.DispatchStatusTracks
                .Select(x => x.DispatchId)
                .ToListAsync(cancellationToken);

            var missingPlans = await _db.DispatchPlans
                .Where(x => !existingDispatchIds.Contains(x.Id))
                .ToListAsync(cancellationToken);

            if (missingPlans.Any())
            {
                var now = DateTime.UtcNow;
                foreach (var plan in missingPlans)
                {
                    var track = new DispatchStatusTrack
                    {
                        DispatchId = plan.Id,
                        DispatchNumber = plan.DispatchNumber,
                        CurrentStatus = DispatchTrackStatus.Ready,
                        WarehouseStatus = WarehouseTrackStatus.Staged,
                        VehicleStatus = VehicleTrackStatus.Pending,
                        TransportStatus = TransportTrackStatus.Pending,
                        DispatchDate = plan.PlannedDispatchDate,
                        ExpectedDelivery = plan.PlannedDispatchDate.AddDays(1),
                        DelayHours = 0,
                        Remarks = string.Empty,
                        Notes = string.Empty,
                        CreatedBy = plan.CreatedBy,
                        CreatedAt = plan.CreatedAt,
                        UpdatedBy = plan.UpdatedBy,
                        UpdatedAt = now
                    };

                    track.Timeline.Add(new DispatchStatusTimelineEvent
                    {
                        EventId = Guid.NewGuid().ToString(),
                        Date = now,
                        User = plan.CreatedBy,
                        Action = "Tracking initialized",
                        ToStatus = track.CurrentStatus == DispatchTrackStatus.InTransit ? "In Transit" : track.CurrentStatus.ToString(),
                        Remarks = "Auto-synced from dispatch plan"
                    });

                    _db.DispatchStatusTracks.Add(track);
                }

                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        private static DispatchStatusDto MapToDto(DispatchStatusTrack s)
        {
            return new DispatchStatusDto
            {
                Id = s.Id,
                DispatchId = s.DispatchId,
                DispatchNumber = s.DispatchNumber,
                CurrentStatus = s.CurrentStatus,
                WarehouseStatus = s.WarehouseStatus,
                VehicleStatus = s.VehicleStatus,
                TransportStatus = s.TransportStatus,
                DispatchDate = s.DispatchDate.ToString("yyyy-MM-dd"),
                ExpectedDelivery = s.ExpectedDelivery.ToString("yyyy-MM-dd"),
                ActualDelivery = s.ActualDelivery?.ToString("yyyy-MM-dd"),
                DelayHours = s.DelayHours,
                Remarks = s.Remarks,
                Notes = s.Notes,
                CreatedBy = s.CreatedBy,
                CreatedAt = s.CreatedAt.ToString("o"),
                UpdatedBy = s.UpdatedBy,
                UpdatedAt = s.UpdatedAt.ToString("o"),
                Timeline = s.Timeline.OrderBy(x => x.Date).Select(x => new DispatchTimelineEventDto
                {
                    Id = x.EventId,
                    Date = x.Date.ToString("o"),
                    User = x.User,
                    Action = x.Action,
                    FromStatus = x.FromStatus,
                    ToStatus = x.ToStatus,
                    Remarks = x.Remarks
                }).ToList()
            };
        }
    }
}
