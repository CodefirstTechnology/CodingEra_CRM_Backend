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
    public class DispatchPlanningService : IDispatchPlanningService
    {
        private readonly ERPDbContext _db;
        private readonly DispatchPlanningNumberingService _numbering;

        public DispatchPlanningService(ERPDbContext db, DispatchPlanningNumberingService numbering)
        {
            _db = db;
            _numbering = numbering;
        }

        public async Task<List<DispatchPlanListItemDto>> GetDispatchPlansAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            var q = _db.DispatchPlans
                .AsNoTracking()
                .Include(x => x.Items)
                .AsQueryable();

            if (query != null)
            {
                if (!string.IsNullOrWhiteSpace(query.Search))
                {
                    var s = query.Search.Trim().ToLower();
                    q = q.Where(x => x.DispatchNumber.ToLower().Contains(s) ||
                                     x.CustomerName.ToLower().Contains(s) ||
                                     x.SalesOrderNumber.ToLower().Contains(s) ||
                                     x.WarehouseName.ToLower().Contains(s));
                }

                if (!string.IsNullOrWhiteSpace(query.Status))
                {
                    var statusStr = query.Status.Trim();
                    if (string.Equals(statusStr, "Ready For Dispatch", StringComparison.OrdinalIgnoreCase))
                    {
                        q = q.Where(x => x.Status == DispatchPlanStatus.ReadyForDispatch);
                    }
                    else if (Enum.TryParse<DispatchPlanStatus>(statusStr.Replace(" ", ""), true, out var st))
                    {
                        q = q.Where(x => x.Status == st);
                    }
                }

                if (query.CustomerId.HasValue && query.CustomerId.Value > 0)
                {
                    q = q.Where(x => x.CustomerId == query.CustomerId.Value);
                }

                if (query.DispatchId.HasValue && query.DispatchId.Value > 0)
                {
                    q = q.Where(x => x.Id == query.DispatchId.Value);
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

            return list.Select(x => new DispatchPlanListItemDto
            {
                Id = x.Id,
                DispatchNumber = x.DispatchNumber,
                DispatchDate = x.DispatchDate.ToString("yyyy-MM-dd"),
                CustomerName = x.CustomerName,
                SalesOrderNumber = x.SalesOrderNumber,
                WarehouseName = x.WarehouseName,
                Priority = x.Priority,
                PlannedDispatchDate = x.PlannedDispatchDate.ToString("yyyy-MM-dd"),
                ExpectedDeliveryDate = x.ExpectedDeliveryDate.ToString("yyyy-MM-dd"),
                ItemCount = x.Items.Count,
                VehicleRequired = x.VehicleRequired,
                Status = x.Status
            }).ToList();
        }

        public async Task<DispatchPlanDto> GetDispatchPlanByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var plan = await _db.DispatchPlans
                .AsNoTracking()
                .Include(x => x.Items)
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (plan == null)
                throw new KeyNotFoundException($"Dispatch plan with ID {id} not found.");

            return MapToDto(plan);
        }

        public async Task<DispatchPlanDto> CreateDispatchPlanAsync(DispatchPlanCreateRequestDto payload, string currentUser, CancellationToken cancellationToken = default)
        {
            ValidatePayload(payload);

            var dispatchDate = ParseDate(payload.DispatchDate);
            var plannedDate = ParseDate(payload.PlannedDispatchDate);
            var expectedDate = ParseDate(payload.ExpectedDeliveryDate);

            if (plannedDate > expectedDate)
                throw new ArgumentException("Planned dispatch date must be on or before expected delivery date.");

            var dispatchNumber = await _numbering.NextNumberAsync(cancellationToken);
            var user = string.IsNullOrWhiteSpace(currentUser) ? "System" : currentUser;
            var now = DateTime.UtcNow;

            var plan = new DispatchPlan
            {
                DispatchNumber = dispatchNumber,
                DispatchDate = dispatchDate,
                CustomerId = payload.CustomerId,
                CustomerName = payload.CustomerName.Trim(),
                SalesOrderId = payload.SalesOrderId,
                SalesOrderNumber = payload.SalesOrderNumber.Trim(),
                DeliveryAddress = payload.DeliveryAddress.Trim(),
                WarehouseId = payload.WarehouseId,
                WarehouseName = payload.WarehouseName.Trim(),
                Priority = payload.Priority,
                PlannedDispatchDate = plannedDate,
                ExpectedDeliveryDate = expectedDate,
                VehicleRequired = payload.VehicleRequired,
                Remarks = payload.Remarks?.Trim() ?? string.Empty,
                Notes = payload.Notes?.Trim() ?? string.Empty,
                Status = DispatchPlanStatus.Draft,
                CreatedBy = user,
                CreatedAt = now,
                UpdatedBy = user,
                UpdatedAt = now,
            };

            foreach (var item in payload.Items)
            {
                plan.Items.Add(new DispatchPlanItem
                {
                    FinishedGoodId = item.FinishedGoodId,
                    FinishedGoodCode = item.FinishedGoodCode.Trim(),
                    FinishedGoodName = item.FinishedGoodName.Trim(),
                    BatchNumber = item.BatchNumber.Trim(),
                    Quantity = item.Quantity,
                    Uom = string.IsNullOrWhiteSpace(item.Uom) ? "NOS" : item.Uom.Trim(),
                    WarehouseId = item.WarehouseId > 0 ? item.WarehouseId : payload.WarehouseId,
                    WarehouseName = string.IsNullOrWhiteSpace(item.WarehouseName) ? payload.WarehouseName : item.WarehouseName.Trim(),
                    FinalInspectionId = item.FinalInspectionId,
                    FinalInspectionNumber = item.FinalInspectionNumber,
                    TestCertificateId = item.TestCertificateId,
                    TestCertificateNumber = item.TestCertificateNumber
                });
            }

            plan.Timeline.Add(new DispatchPlanTimelineEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Date = now,
                User = user,
                Action = "Created dispatch plan",
                ToStatus = "Draft",
                Remarks = "Initial draft creation"
            });

            _db.DispatchPlans.Add(plan);
            await _db.SaveChangesAsync(cancellationToken);

            return MapToDto(plan);
        }

        public async Task<DispatchPlanDto> UpdateDispatchPlanAsync(int id, DispatchPlanUpdateRequestDto payload, string currentUser, CancellationToken cancellationToken = default)
        {
            ValidatePayload(payload);

            var plan = await _db.DispatchPlans
                .Include(x => x.Items)
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (plan == null)
                throw new KeyNotFoundException($"Dispatch plan with ID {id} not found.");

            if (plan.Status != DispatchPlanStatus.Draft)
                throw new InvalidOperationException("Only draft dispatch plans can be edited.");

            var dispatchDate = ParseDate(payload.DispatchDate);
            var plannedDate = ParseDate(payload.PlannedDispatchDate);
            var expectedDate = ParseDate(payload.ExpectedDeliveryDate);

            if (plannedDate > expectedDate)
                throw new ArgumentException("Planned dispatch date must be on or before expected delivery date.");

            var user = string.IsNullOrWhiteSpace(currentUser) ? "System" : currentUser;
            var now = DateTime.UtcNow;

            plan.DispatchDate = dispatchDate;
            plan.CustomerId = payload.CustomerId;
            plan.CustomerName = payload.CustomerName.Trim();
            plan.SalesOrderId = payload.SalesOrderId;
            plan.SalesOrderNumber = payload.SalesOrderNumber.Trim();
            plan.DeliveryAddress = payload.DeliveryAddress.Trim();
            plan.WarehouseId = payload.WarehouseId;
            plan.WarehouseName = payload.WarehouseName.Trim();
            plan.Priority = payload.Priority;
            plan.PlannedDispatchDate = plannedDate;
            plan.ExpectedDeliveryDate = expectedDate;
            plan.VehicleRequired = payload.VehicleRequired;
            plan.Remarks = payload.Remarks?.Trim() ?? string.Empty;
            plan.Notes = payload.Notes?.Trim() ?? string.Empty;
            plan.UpdatedBy = user;
            plan.UpdatedAt = now;

            _db.DispatchPlanItems.RemoveRange(plan.Items);
            plan.Items.Clear();

            foreach (var item in payload.Items)
            {
                plan.Items.Add(new DispatchPlanItem
                {
                    DispatchPlanId = plan.Id,
                    FinishedGoodId = item.FinishedGoodId,
                    FinishedGoodCode = item.FinishedGoodCode.Trim(),
                    FinishedGoodName = item.FinishedGoodName.Trim(),
                    BatchNumber = item.BatchNumber.Trim(),
                    Quantity = item.Quantity,
                    Uom = string.IsNullOrWhiteSpace(item.Uom) ? "NOS" : item.Uom.Trim(),
                    WarehouseId = item.WarehouseId > 0 ? item.WarehouseId : payload.WarehouseId,
                    WarehouseName = string.IsNullOrWhiteSpace(item.WarehouseName) ? payload.WarehouseName : item.WarehouseName.Trim(),
                    FinalInspectionId = item.FinalInspectionId,
                    FinalInspectionNumber = item.FinalInspectionNumber,
                    TestCertificateId = item.TestCertificateId,
                    TestCertificateNumber = item.TestCertificateNumber
                });
            }

            plan.Timeline.Add(new DispatchPlanTimelineEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Date = now,
                User = user,
                Action = "Updated dispatch plan",
                Remarks = "Draft updated"
            });

            await _db.SaveChangesAsync(cancellationToken);
            return MapToDto(plan);
        }

        public async Task DeleteDispatchPlanAsync(int id, CancellationToken cancellationToken = default)
        {
            var plan = await _db.DispatchPlans.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (plan == null)
                throw new KeyNotFoundException($"Dispatch plan with ID {id} not found.");

            if (plan.Status != DispatchPlanStatus.Draft)
                throw new InvalidOperationException("Only draft dispatch plans can be deleted.");

            _db.DispatchPlans.Remove(plan);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<DispatchPlanDto> DuplicateDispatchPlanAsync(int id, string currentUser, CancellationToken cancellationToken = default)
        {
            var existing = await _db.DispatchPlans
                .AsNoTracking()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (existing == null)
                throw new KeyNotFoundException($"Dispatch plan with ID {id} not found.");

            var dispatchNumber = await _numbering.NextNumberAsync(cancellationToken);
            var user = string.IsNullOrWhiteSpace(currentUser) ? "System" : currentUser;
            var now = DateTime.UtcNow;

            var copy = new DispatchPlan
            {
                DispatchNumber = dispatchNumber,
                DispatchDate = now,
                CustomerId = existing.CustomerId,
                CustomerName = existing.CustomerName,
                SalesOrderId = existing.SalesOrderId,
                SalesOrderNumber = existing.SalesOrderNumber,
                DeliveryAddress = existing.DeliveryAddress,
                WarehouseId = existing.WarehouseId,
                WarehouseName = existing.WarehouseName,
                Priority = existing.Priority,
                PlannedDispatchDate = existing.PlannedDispatchDate,
                ExpectedDeliveryDate = existing.ExpectedDeliveryDate,
                VehicleRequired = existing.VehicleRequired,
                Remarks = existing.Remarks,
                Notes = $"Duplicated from {existing.DispatchNumber}. {existing.Notes}".Trim(),
                Status = DispatchPlanStatus.Draft,
                CreatedBy = user,
                CreatedAt = now,
                UpdatedBy = user,
                UpdatedAt = now
            };

            foreach (var item in existing.Items)
            {
                copy.Items.Add(new DispatchPlanItem
                {
                    FinishedGoodId = item.FinishedGoodId,
                    FinishedGoodCode = item.FinishedGoodCode,
                    FinishedGoodName = item.FinishedGoodName,
                    BatchNumber = item.BatchNumber,
                    Quantity = item.Quantity,
                    Uom = item.Uom,
                    WarehouseId = item.WarehouseId,
                    WarehouseName = item.WarehouseName,
                    FinalInspectionId = item.FinalInspectionId,
                    FinalInspectionNumber = item.FinalInspectionNumber,
                    TestCertificateId = item.TestCertificateId,
                    TestCertificateNumber = item.TestCertificateNumber
                });
            }

            copy.Timeline.Add(new DispatchPlanTimelineEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Date = now,
                User = user,
                Action = $"Duplicated from {existing.DispatchNumber}",
                ToStatus = "Draft"
            });

            _db.DispatchPlans.Add(copy);
            await _db.SaveChangesAsync(cancellationToken);

            return MapToDto(copy);
        }

        public async Task<DispatchPlanDto> PlanDispatchAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default)
        {
            return await TransitionStatusAsync(id, DispatchPlanStatus.Draft, DispatchPlanStatus.Planned, "Marked as planned", payload, currentUser, cancellationToken);
        }

        public async Task<DispatchPlanDto> ApproveDispatchAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default)
        {
            return await TransitionStatusAsync(id, DispatchPlanStatus.Planned, DispatchPlanStatus.Approved, "Approved dispatch", payload, currentUser, cancellationToken);
        }

        public async Task<DispatchPlanDto> MarkReadyForDispatchAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default)
        {
            return await TransitionStatusAsync(id, DispatchPlanStatus.Approved, DispatchPlanStatus.ReadyForDispatch, "FG staged — ready for dispatch", payload, currentUser, cancellationToken);
        }

        public async Task<DispatchPlanDto> CloseDispatchAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default)
        {
            return await TransitionStatusAsync(id, DispatchPlanStatus.ReadyForDispatch, DispatchPlanStatus.Closed, "Closed dispatch", payload, currentUser, cancellationToken);
        }

        public async Task<DispatchPlanDashboardDto> GetDispatchPlanDashboardAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var todaysCount = await _db.DispatchPlans
                .CountAsync(x => x.DispatchDate >= today && x.DispatchDate < tomorrow, cancellationToken);

            var plannedCount = await _db.DispatchPlans
                .CountAsync(x => x.Status == DispatchPlanStatus.Planned, cancellationToken);

            var readyCount = await _db.DispatchPlans
                .CountAsync(x => x.Status == DispatchPlanStatus.ReadyForDispatch, cancellationToken);

            var completedCount = await _db.DispatchPlans
                .CountAsync(x => x.Status == DispatchPlanStatus.Closed, cancellationToken);

            return new DispatchPlanDashboardDto
            {
                TodaysDispatches = todaysCount,
                Planned = plannedCount,
                Ready = readyCount,
                Completed = completedCount
            };
        }

        private async Task<DispatchPlanDto> TransitionStatusAsync(
            int id,
            DispatchPlanStatus fromStatus,
            DispatchPlanStatus toStatus,
            string defaultAction,
            StatusActionRequestDto? payload,
            string currentUser,
            CancellationToken cancellationToken)
        {
            var plan = await _db.DispatchPlans
                .Include(x => x.Items)
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (plan == null)
                throw new KeyNotFoundException($"Dispatch plan with ID {id} not found.");

            if (plan.Status != fromStatus)
                throw new InvalidOperationException($"Invalid status transition from {plan.Status} to {toStatus}. Expected current status to be {fromStatus}.");

            var user = !string.IsNullOrWhiteSpace(payload?.ApprovedBy)
                ? payload.ApprovedBy.Trim()
                : !string.IsNullOrWhiteSpace(currentUser)
                    ? currentUser
                    : "System";

            var now = DateTime.UtcNow;
            var fromStatusStr = fromStatus == DispatchPlanStatus.ReadyForDispatch ? "Ready For Dispatch" : fromStatus.ToString();
            var toStatusStr = toStatus == DispatchPlanStatus.ReadyForDispatch ? "Ready For Dispatch" : toStatus.ToString();

            plan.Status = toStatus;
            plan.UpdatedBy = user;
            plan.UpdatedAt = now;

            plan.Timeline.Add(new DispatchPlanTimelineEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Date = now,
                User = user,
                Action = defaultAction,
                FromStatus = fromStatusStr,
                ToStatus = toStatusStr,
                Remarks = payload?.Remarks?.Trim()
            });

            await _db.SaveChangesAsync(cancellationToken);
            return MapToDto(plan);
        }

        private static void ValidatePayload(DispatchPlanCreateRequestDto payload)
        {
            if (string.IsNullOrWhiteSpace(payload.DispatchDate))
                throw new ArgumentException("Dispatch date is required.");
            if (string.IsNullOrWhiteSpace(payload.CustomerName))
                throw new ArgumentException("Customer name is required.");
            if (string.IsNullOrWhiteSpace(payload.SalesOrderNumber))
                throw new ArgumentException("Sales order number is required.");
            if (string.IsNullOrWhiteSpace(payload.DeliveryAddress))
                throw new ArgumentException("Delivery address is required.");
            if (string.IsNullOrWhiteSpace(payload.WarehouseName))
                throw new ArgumentException("Warehouse name is required.");
            if (string.IsNullOrWhiteSpace(payload.PlannedDispatchDate))
                throw new ArgumentException("Planned dispatch date is required.");
            if (string.IsNullOrWhiteSpace(payload.ExpectedDeliveryDate))
                throw new ArgumentException("Expected delivery date is required.");

            if (payload.Items == null || payload.Items.Count == 0)
                throw new ArgumentException("At least one dispatch item is required.");

            foreach (var item in payload.Items)
            {
                if (string.IsNullOrWhiteSpace(item.FinishedGoodCode))
                    throw new ArgumentException("Finished good code is required for each line item.");
                if (string.IsNullOrWhiteSpace(item.FinishedGoodName))
                    throw new ArgumentException("Finished good name is required for each line item.");
                if (item.Quantity < 1)
                    throw new ArgumentException("Quantity must be at least 1 for each line item.");
            }
        }

        private static DateTime ParseDate(string dateStr)
        {
            if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
            {
                return DateTime.SpecifyKind(dt.Date, DateTimeKind.Utc);
            }
            return DateTime.UtcNow.Date;
        }

        private static DispatchPlanDto MapToDto(DispatchPlan p)
        {
            return new DispatchPlanDto
            {
                Id = p.Id,
                DispatchNumber = p.DispatchNumber,
                DispatchDate = p.DispatchDate.ToString("yyyy-MM-dd"),
                CustomerId = p.CustomerId,
                CustomerName = p.CustomerName,
                SalesOrderId = p.SalesOrderId,
                SalesOrderNumber = p.SalesOrderNumber,
                DeliveryAddress = p.DeliveryAddress,
                WarehouseId = p.WarehouseId,
                WarehouseName = p.WarehouseName,
                Priority = p.Priority,
                PlannedDispatchDate = p.PlannedDispatchDate.ToString("yyyy-MM-dd"),
                ExpectedDeliveryDate = p.ExpectedDeliveryDate.ToString("yyyy-MM-dd"),
                VehicleRequired = p.VehicleRequired,
                Remarks = p.Remarks,
                Notes = p.Notes,
                Status = p.Status,
                VehicleAssignmentId = p.VehicleAssignmentId,
                TransportId = p.TransportId,
                CreatedBy = p.CreatedBy,
                CreatedAt = p.CreatedAt.ToString("o"),
                UpdatedBy = p.UpdatedBy,
                UpdatedAt = p.UpdatedAt.ToString("o"),
                Items = p.Items.OrderBy(i => i.Id).Select(i => new DispatchPlanItemDto
                {
                    Id = i.Id.ToString(),
                    FinishedGoodId = i.FinishedGoodId,
                    FinishedGoodCode = i.FinishedGoodCode,
                    FinishedGoodName = i.FinishedGoodName,
                    BatchNumber = i.BatchNumber,
                    Quantity = i.Quantity,
                    Uom = i.Uom,
                    WarehouseId = i.WarehouseId,
                    WarehouseName = i.WarehouseName,
                    FinalInspectionId = i.FinalInspectionId,
                    FinalInspectionNumber = i.FinalInspectionNumber,
                    TestCertificateId = i.TestCertificateId,
                    TestCertificateNumber = i.TestCertificateNumber
                }).ToList(),
                Attachments = p.Attachments.OrderByDescending(a => a.UploadedAt).Select(a => new DispatchAttachmentDto
                {
                    Id = a.AttachmentId,
                    Name = a.Name,
                    SizeKb = a.SizeKb,
                    UploadedBy = a.UploadedBy,
                    UploadedAt = a.UploadedAt.ToString("o"),
                    Kind = a.Kind
                }).ToList(),
                Timeline = p.Timeline.OrderBy(t => t.Date).Select(t => new DispatchTimelineEventDto
                {
                    Id = t.EventId,
                    Date = t.Date.ToString("o"),
                    User = t.User,
                    Action = t.Action,
                    FromStatus = t.FromStatus,
                    ToStatus = t.ToStatus,
                    Remarks = t.Remarks
                }).ToList()
            };
        }
    }
}
