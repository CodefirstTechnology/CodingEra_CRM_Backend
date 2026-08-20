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
    public class VehicleAssignmentService : IVehicleAssignmentService
    {
        private readonly ERPDbContext _db;
        private readonly VehicleAssignmentNumberingService _numbering;
        private readonly TransportNumberingService _transportNumbering;

        public VehicleAssignmentService(
            ERPDbContext db,
            VehicleAssignmentNumberingService numbering,
            TransportNumberingService transportNumbering)
        {
            _db = db;
            _numbering = numbering;
            _transportNumbering = transportNumbering;
        }

        public async Task<List<VehicleAssignmentListItemDto>> GetVehicleAssignmentsAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            var q = _db.VehicleAssignments.AsNoTracking().AsQueryable();

            if (query != null)
            {
                if (!string.IsNullOrWhiteSpace(query.Search))
                {
                    var s = query.Search.Trim().ToLower();
                    q = q.Where(x => x.AssignmentNumber.ToLower().Contains(s) ||
                                     x.DispatchNumber.ToLower().Contains(s) ||
                                     x.VehicleNumber.ToLower().Contains(s) ||
                                     x.DriverName.ToLower().Contains(s) ||
                                     x.TransportCompanyName.ToLower().Contains(s));
                }

                if (!string.IsNullOrWhiteSpace(query.Status))
                {
                    var statusStr = query.Status.Trim();
                    if (Enum.TryParse<VehicleAssignmentStatus>(statusStr.Replace(" ", ""), true, out var st))
                    {
                        q = q.Where(x => x.Status == st);
                    }
                }

                if (query.DispatchId.HasValue && query.DispatchId.Value > 0)
                {
                    q = q.Where(x => x.DispatchId == query.DispatchId.Value);
                }

                if (!string.IsNullOrWhiteSpace(query.DateFrom) && DateTime.TryParse(query.DateFrom, out var df))
                {
                    q = q.Where(x => x.LoadingDate >= DateTime.SpecifyKind(df.Date, DateTimeKind.Utc));
                }

                if (!string.IsNullOrWhiteSpace(query.DateTo) && DateTime.TryParse(query.DateTo, out var dt))
                {
                    q = q.Where(x => x.LoadingDate <= DateTime.SpecifyKind(dt.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc));
                }
            }

            var list = await q.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

            return list.Select(x => new VehicleAssignmentListItemDto
            {
                Id = x.Id,
                AssignmentNumber = x.AssignmentNumber,
                DispatchNumber = x.DispatchNumber,
                VehicleNumber = x.VehicleNumber,
                VehicleType = x.VehicleType,
                DriverName = x.DriverName,
                TransportCompanyName = x.TransportCompanyName,
                LoadingDate = x.LoadingDate.ToString("yyyy-MM-dd"),
                AssignedQuantity = x.AssignedQuantity,
                Status = x.Status
            }).ToList();
        }

        public async Task<VehicleAssignmentDto> GetVehicleAssignmentByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var assignment = await _db.VehicleAssignments
                .AsNoTracking()
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (assignment == null)
                throw new KeyNotFoundException($"Vehicle assignment with ID {id} not found.");

            return MapToDto(assignment);
        }

        public async Task<VehicleAssignmentDto> CreateVehicleAssignmentAsync(VehicleAssignmentCreateRequestDto payload, string currentUser, CancellationToken cancellationToken = default)
        {
            ValidatePayload(payload);

            var loadingDate = ParseDate(payload.LoadingDate);
            var expectedDeparture = ParseDate(payload.ExpectedDeparture);
            var assignmentNumber = await _numbering.NextNumberAsync(cancellationToken);
            var user = string.IsNullOrWhiteSpace(currentUser) ? "System" : currentUser;
            var now = DateTime.UtcNow;

            var assignment = new VehicleAssignment
            {
                AssignmentNumber = assignmentNumber,
                DispatchId = payload.DispatchId,
                DispatchNumber = payload.DispatchNumber.Trim(),
                VehicleId = payload.VehicleId,
                VehicleName = payload.VehicleName.Trim(),
                VehicleNumber = payload.VehicleNumber.Trim(),
                VehicleType = payload.VehicleType,
                DriverName = payload.DriverName.Trim(),
                DriverContact = payload.DriverContact.Trim(),
                TransportCompanyId = payload.TransportCompanyId,
                TransportCompanyName = payload.TransportCompanyName.Trim(),
                LoadingDate = loadingDate,
                LoadingTime = string.IsNullOrWhiteSpace(payload.LoadingTime) ? "09:00" : payload.LoadingTime.Trim(),
                ExpectedDeparture = expectedDeparture,
                Capacity = payload.Capacity,
                AssignedQuantity = payload.AssignedQuantity,
                Remarks = payload.Remarks?.Trim() ?? string.Empty,
                Notes = payload.Notes?.Trim() ?? string.Empty,
                Status = VehicleAssignmentStatus.Draft,
                CreatedBy = user,
                CreatedAt = now,
                UpdatedBy = user,
                UpdatedAt = now
            };

            assignment.Timeline.Add(new VehicleAssignmentTimelineEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Date = now,
                User = user,
                Action = "Vehicle assignment created",
                ToStatus = "Draft",
                Remarks = "Initial creation"
            });

            _db.VehicleAssignments.Add(assignment);
            await _db.SaveChangesAsync(cancellationToken);

            return MapToDto(assignment);
        }

        public async Task<VehicleAssignmentDto> UpdateVehicleAssignmentAsync(int id, VehicleAssignmentUpdateRequestDto payload, string currentUser, CancellationToken cancellationToken = default)
        {
            ValidatePayload(payload);

            var assignment = await _db.VehicleAssignments
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (assignment == null)
                throw new KeyNotFoundException($"Vehicle assignment with ID {id} not found.");

            if (assignment.Status != VehicleAssignmentStatus.Draft)
                throw new InvalidOperationException("Only draft vehicle assignments can be edited.");

            var loadingDate = ParseDate(payload.LoadingDate);
            var expectedDeparture = ParseDate(payload.ExpectedDeparture);
            var user = string.IsNullOrWhiteSpace(currentUser) ? "System" : currentUser;
            var now = DateTime.UtcNow;

            assignment.DispatchId = payload.DispatchId;
            assignment.DispatchNumber = payload.DispatchNumber.Trim();
            assignment.VehicleId = payload.VehicleId;
            assignment.VehicleName = payload.VehicleName.Trim();
            assignment.VehicleNumber = payload.VehicleNumber.Trim();
            assignment.VehicleType = payload.VehicleType;
            assignment.DriverName = payload.DriverName.Trim();
            assignment.DriverContact = payload.DriverContact.Trim();
            assignment.TransportCompanyId = payload.TransportCompanyId;
            assignment.TransportCompanyName = payload.TransportCompanyName.Trim();
            assignment.LoadingDate = loadingDate;
            assignment.LoadingTime = string.IsNullOrWhiteSpace(payload.LoadingTime) ? "09:00" : payload.LoadingTime.Trim();
            assignment.ExpectedDeparture = expectedDeparture;
            assignment.Capacity = payload.Capacity;
            assignment.AssignedQuantity = payload.AssignedQuantity;
            assignment.Remarks = payload.Remarks?.Trim() ?? string.Empty;
            assignment.Notes = payload.Notes?.Trim() ?? string.Empty;
            assignment.UpdatedBy = user;
            assignment.UpdatedAt = now;

            assignment.Timeline.Add(new VehicleAssignmentTimelineEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Date = now,
                User = user,
                Action = "Assignment updated",
                Remarks = "Draft updated"
            });

            await _db.SaveChangesAsync(cancellationToken);
            return MapToDto(assignment);
        }

        public async Task DeleteVehicleAssignmentAsync(int id, CancellationToken cancellationToken = default)
        {
            var assignment = await _db.VehicleAssignments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (assignment == null)
                throw new KeyNotFoundException($"Vehicle assignment with ID {id} not found.");

            if (assignment.Status != VehicleAssignmentStatus.Draft)
                throw new InvalidOperationException("Only draft vehicle assignments can be deleted.");

            _db.VehicleAssignments.Remove(assignment);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<VehicleAssignmentDto> ChangeVehicleAsync(int id, VehicleAssignmentUpdateRequestDto payload, string currentUser, CancellationToken cancellationToken = default)
        {
            ValidatePayload(payload);

            var assignment = await _db.VehicleAssignments
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (assignment == null)
                throw new KeyNotFoundException($"Vehicle assignment with ID {id} not found.");

            if (assignment.Status == VehicleAssignmentStatus.Completed ||
                assignment.Status == VehicleAssignmentStatus.Cancelled ||
                assignment.Status == VehicleAssignmentStatus.Dispatched)
            {
                throw new InvalidOperationException($"Cannot change vehicle in status {assignment.Status}.");
            }

            var loadingDate = ParseDate(payload.LoadingDate);
            var expectedDeparture = ParseDate(payload.ExpectedDeparture);
            var user = string.IsNullOrWhiteSpace(currentUser) ? "System" : currentUser;
            var now = DateTime.UtcNow;

            assignment.VehicleId = payload.VehicleId;
            assignment.VehicleName = payload.VehicleName.Trim();
            assignment.VehicleNumber = payload.VehicleNumber.Trim();
            assignment.VehicleType = payload.VehicleType;
            assignment.DriverName = payload.DriverName.Trim();
            assignment.DriverContact = payload.DriverContact.Trim();
            assignment.TransportCompanyId = payload.TransportCompanyId;
            assignment.TransportCompanyName = payload.TransportCompanyName.Trim();
            assignment.LoadingDate = loadingDate;
            assignment.LoadingTime = string.IsNullOrWhiteSpace(payload.LoadingTime) ? "09:00" : payload.LoadingTime.Trim();
            assignment.ExpectedDeparture = expectedDeparture;
            assignment.Capacity = payload.Capacity;
            assignment.AssignedQuantity = payload.AssignedQuantity;
            if (!string.IsNullOrWhiteSpace(payload.Remarks))
                assignment.Remarks = payload.Remarks.Trim();
            if (!string.IsNullOrWhiteSpace(payload.Notes))
                assignment.Notes = payload.Notes.Trim();

            assignment.UpdatedBy = user;
            assignment.UpdatedAt = now;

            assignment.Timeline.Add(new VehicleAssignmentTimelineEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Date = now,
                User = user,
                Action = $"Vehicle changed to {payload.VehicleNumber.Trim()}",
                FromStatus = assignment.Status.ToString(),
                ToStatus = assignment.Status.ToString(),
                Remarks = payload.Remarks?.Trim()
            });

            await _db.SaveChangesAsync(cancellationToken);
            return MapToDto(assignment);
        }

        public async Task<VehicleAssignmentDto> CancelVehicleAssignmentAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(payload?.Remarks))
                throw new ArgumentException("Cancellation reason is required.");

            return await TransitionStatusAsync(
                id,
                new[] { VehicleAssignmentStatus.Draft, VehicleAssignmentStatus.Assigned, VehicleAssignmentStatus.Loaded },
                VehicleAssignmentStatus.Cancelled,
                "Assignment cancelled",
                payload,
                currentUser,
                cancellationToken);
        }

        public async Task<VehicleAssignmentDto> MarkLoadedAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default)
        {
            return await TransitionStatusAsync(
                id,
                new[] { VehicleAssignmentStatus.Assigned },
                VehicleAssignmentStatus.Loaded,
                "Vehicle loaded",
                payload,
                currentUser,
                cancellationToken);
        }

        public async Task<VehicleAssignmentDto> MarkVehicleDispatchedAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default)
        {
            return await TransitionStatusAsync(
                id,
                new[] { VehicleAssignmentStatus.Loaded },
                VehicleAssignmentStatus.Dispatched,
                "Vehicle dispatched",
                payload,
                currentUser,
                cancellationToken);
        }

        public async Task<VehicleAssignmentDto> CompleteVehicleAssignmentAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(payload?.Remarks))
                throw new ArgumentException("Completion remarks are required.");

            return await TransitionStatusAsync(
                id,
                new[] { VehicleAssignmentStatus.Dispatched },
                VehicleAssignmentStatus.Completed,
                "Assignment completed",
                payload,
                currentUser,
                cancellationToken);
        }

        public async Task<TransportDto> GenerateTransportAsync(int vehicleAssignmentId, string currentUser, CancellationToken cancellationToken = default)
        {
            var assignment = await _db.VehicleAssignments
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == vehicleAssignmentId, cancellationToken);

            if (assignment == null)
                throw new KeyNotFoundException($"Vehicle assignment with ID {vehicleAssignmentId} not found.");

            if (assignment.TransportId.HasValue && assignment.TransportId.Value > 0)
            {
                var existing = await _db.TransportDetails.FirstOrDefaultAsync(x => x.Id == assignment.TransportId.Value, cancellationToken);
                if (existing != null)
                {
                    return MapTransportToDto(existing);
                }
            }

            var plan = await _db.DispatchPlans.FirstOrDefaultAsync(x => x.Id == assignment.DispatchId, cancellationToken);
            var transportNumber = await _transportNumbering.NextNumberAsync(cancellationToken);
            var user = string.IsNullOrWhiteSpace(currentUser) ? "System" : currentUser;
            var now = DateTime.UtcNow;

            var transport = new TransportDetail
            {
                TransportNumber = transportNumber,
                DispatchId = assignment.DispatchId,
                DispatchNumber = assignment.DispatchNumber,
                VehicleAssignmentId = assignment.Id,
                AssignmentNumber = assignment.AssignmentNumber,
                VehicleNumber = assignment.VehicleNumber,
                DriverName = assignment.DriverName,
                TransportCompanyName = assignment.TransportCompanyName,
                Mode = TransportMode.Road,
                Route = plan != null ? $"{plan.WarehouseName} → {plan.CustomerName}" : "Route not specified",
                Source = plan?.WarehouseName ?? "Source Warehouse",
                Destination = plan?.DeliveryAddress ?? "Customer Destination",
                DistanceKm = 0,
                EstimatedTimeHours = 0,
                ActualDeparture = null,
                ActualArrival = null,
                FuelNotes = string.Empty,
                Remarks = $"Generated from {assignment.AssignmentNumber}",
                Notes = string.Empty,
                Status = TransportStatus.Draft,
                CreatedBy = user,
                CreatedAt = now,
                UpdatedBy = user,
                UpdatedAt = now
            };

            _db.TransportDetails.Add(transport);
            await _db.SaveChangesAsync(cancellationToken);

            assignment.TransportId = transport.Id;
            assignment.UpdatedBy = user;
            assignment.UpdatedAt = now;
            assignment.Timeline.Add(new VehicleAssignmentTimelineEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Date = now,
                User = user,
                Action = $"Transport {transport.TransportNumber} generated",
                FromStatus = assignment.Status.ToString(),
                ToStatus = assignment.Status.ToString(),
                Remarks = $"Transport ID {transport.Id}"
            });

            if (plan != null)
            {
                plan.TransportId = transport.Id;
                plan.UpdatedBy = user;
                plan.UpdatedAt = now;
                plan.Timeline.Add(new DispatchPlanTimelineEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    Date = now,
                    User = user,
                    Action = $"Transport {transport.TransportNumber} linked",
                    FromStatus = plan.Status == DispatchPlanStatus.ReadyForDispatch ? "Ready For Dispatch" : plan.Status.ToString(),
                    ToStatus = plan.Status == DispatchPlanStatus.ReadyForDispatch ? "Ready For Dispatch" : plan.Status.ToString(),
                    Remarks = $"Transport ID {transport.Id}"
                });
            }

            await _db.SaveChangesAsync(cancellationToken);

            return MapTransportToDto(transport);
        }

        public async Task<VehicleAssignmentDashboardDto> GetVehicleAssignmentDashboardAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var vehiclesAssigned = await _db.VehicleAssignments
                .CountAsync(x => x.Status == VehicleAssignmentStatus.Assigned || x.Status == VehicleAssignmentStatus.Loaded, cancellationToken);

            var loadingToday = await _db.VehicleAssignments
                .CountAsync(x => x.LoadingDate >= today && x.LoadingDate < tomorrow, cancellationToken);

            var pendingAssignment = await _db.VehicleAssignments
                .CountAsync(x => x.Status == VehicleAssignmentStatus.Draft, cancellationToken);

            var completedDeliveries = await _db.VehicleAssignments
                .CountAsync(x => x.Status == VehicleAssignmentStatus.Completed, cancellationToken);

            return new VehicleAssignmentDashboardDto
            {
                VehiclesAssigned = vehiclesAssigned,
                LoadingToday = loadingToday,
                PendingAssignment = pendingAssignment,
                CompletedDeliveries = completedDeliveries
            };
        }

        public async Task<VehicleAssignmentDto> AssignVehicleToDispatchAsync(int dispatchId, VehicleAssignmentCreateRequestDto payload, string currentUser, CancellationToken cancellationToken = default)
        {
            var plan = await _db.DispatchPlans
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == dispatchId, cancellationToken);

            if (plan == null)
                throw new KeyNotFoundException($"Dispatch plan with ID {dispatchId} not found.");

            ValidatePayload(payload);

            var loadingDate = ParseDate(payload.LoadingDate);
            var expectedDeparture = ParseDate(payload.ExpectedDeparture);
            var assignmentNumber = await _numbering.NextNumberAsync(cancellationToken);
            var user = string.IsNullOrWhiteSpace(currentUser) ? "System" : currentUser;
            var now = DateTime.UtcNow;

            var assignment = new VehicleAssignment
            {
                AssignmentNumber = assignmentNumber,
                DispatchId = dispatchId,
                DispatchNumber = plan.DispatchNumber,
                VehicleId = payload.VehicleId,
                VehicleName = payload.VehicleName.Trim(),
                VehicleNumber = payload.VehicleNumber.Trim(),
                VehicleType = payload.VehicleType,
                DriverName = payload.DriverName.Trim(),
                DriverContact = payload.DriverContact.Trim(),
                TransportCompanyId = payload.TransportCompanyId,
                TransportCompanyName = payload.TransportCompanyName.Trim(),
                LoadingDate = loadingDate,
                LoadingTime = string.IsNullOrWhiteSpace(payload.LoadingTime) ? "09:00" : payload.LoadingTime.Trim(),
                ExpectedDeparture = expectedDeparture,
                Capacity = payload.Capacity,
                AssignedQuantity = payload.AssignedQuantity,
                Remarks = payload.Remarks?.Trim() ?? string.Empty,
                Notes = payload.Notes?.Trim() ?? string.Empty,
                Status = VehicleAssignmentStatus.Assigned,
                CreatedBy = user,
                CreatedAt = now,
                UpdatedBy = user,
                UpdatedAt = now
            };

            assignment.Timeline.Add(new VehicleAssignmentTimelineEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Date = now,
                User = user,
                Action = $"Assigned to {plan.DispatchNumber}",
                ToStatus = "Assigned",
                Remarks = "Assigned via Dispatch Plan"
            });

            _db.VehicleAssignments.Add(assignment);
            await _db.SaveChangesAsync(cancellationToken);

            plan.VehicleAssignmentId = assignment.Id;
            plan.UpdatedBy = user;
            plan.UpdatedAt = now;
            plan.Timeline.Add(new DispatchPlanTimelineEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Date = now,
                User = user,
                Action = $"Vehicle {assignment.AssignmentNumber} assigned",
                FromStatus = plan.Status == DispatchPlanStatus.ReadyForDispatch ? "Ready For Dispatch" : plan.Status.ToString(),
                ToStatus = plan.Status == DispatchPlanStatus.ReadyForDispatch ? "Ready For Dispatch" : plan.Status.ToString(),
                Remarks = $"Vehicle Number: {assignment.VehicleNumber}"
            });

            await _db.SaveChangesAsync(cancellationToken);

            return MapToDto(assignment);
        }

        private async Task<VehicleAssignmentDto> TransitionStatusAsync(
            int id,
            VehicleAssignmentStatus[] allowedFromStatuses,
            VehicleAssignmentStatus toStatus,
            string defaultAction,
            StatusActionRequestDto? payload,
            string currentUser,
            CancellationToken cancellationToken)
        {
            var assignment = await _db.VehicleAssignments
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (assignment == null)
                throw new KeyNotFoundException($"Vehicle assignment with ID {id} not found.");

            if (!allowedFromStatuses.Contains(assignment.Status))
            {
                var allowed = string.Join(", ", allowedFromStatuses);
                throw new InvalidOperationException($"Invalid status transition from {assignment.Status} to {toStatus}. Allowed current status: [{allowed}].");
            }

            var user = !string.IsNullOrWhiteSpace(payload?.ApprovedBy)
                ? payload.ApprovedBy.Trim()
                : !string.IsNullOrWhiteSpace(currentUser)
                    ? currentUser
                    : "System";

            var now = DateTime.UtcNow;
            var fromStatusStr = assignment.Status.ToString();
            var toStatusStr = toStatus.ToString();

            assignment.Status = toStatus;
            assignment.UpdatedBy = user;
            assignment.UpdatedAt = now;

            assignment.Timeline.Add(new VehicleAssignmentTimelineEvent
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
            return MapToDto(assignment);
        }

        private static void ValidatePayload(VehicleAssignmentCreateRequestDto payload)
        {
            if (string.IsNullOrWhiteSpace(payload.VehicleNumber))
                throw new ArgumentException("Vehicle number is required.");
            if (string.IsNullOrWhiteSpace(payload.DriverName))
                throw new ArgumentException("Driver name is required.");
            if (string.IsNullOrWhiteSpace(payload.DriverContact))
                throw new ArgumentException("Driver contact is required.");
            if (string.IsNullOrWhiteSpace(payload.TransportCompanyName))
                throw new ArgumentException("Transport company name is required.");
            if (string.IsNullOrWhiteSpace(payload.LoadingDate))
                throw new ArgumentException("Loading date is required.");

            if (payload.Capacity <= 0)
                throw new ArgumentException("Capacity must be greater than 0.");
            if (payload.AssignedQuantity <= 0)
                throw new ArgumentException("Assigned quantity must be greater than 0.");
            if (payload.AssignedQuantity > payload.Capacity)
                throw new ArgumentException("Assigned quantity cannot exceed vehicle capacity.");
        }

        private static DateTime ParseDate(string? dateStr)
        {
            if (!string.IsNullOrWhiteSpace(dateStr) &&
                DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
            {
                return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }
            return DateTime.UtcNow;
        }

        private static VehicleAssignmentDto MapToDto(VehicleAssignment a)
        {
            return new VehicleAssignmentDto
            {
                Id = a.Id,
                AssignmentNumber = a.AssignmentNumber,
                DispatchId = a.DispatchId,
                DispatchNumber = a.DispatchNumber,
                VehicleId = a.VehicleId,
                VehicleName = a.VehicleName,
                VehicleNumber = a.VehicleNumber,
                VehicleType = a.VehicleType,
                DriverName = a.DriverName,
                DriverContact = a.DriverContact,
                TransportCompanyId = a.TransportCompanyId,
                TransportCompanyName = a.TransportCompanyName,
                LoadingDate = a.LoadingDate.ToString("yyyy-MM-dd"),
                LoadingTime = a.LoadingTime,
                ExpectedDeparture = a.ExpectedDeparture.ToString("o"),
                Capacity = a.Capacity,
                AssignedQuantity = a.AssignedQuantity,
                Remarks = a.Remarks,
                Notes = a.Notes,
                Status = a.Status,
                TransportId = a.TransportId,
                CreatedBy = a.CreatedBy,
                CreatedAt = a.CreatedAt.ToString("o"),
                UpdatedBy = a.UpdatedBy,
                UpdatedAt = a.UpdatedAt.ToString("o"),
                Attachments = a.Attachments.OrderByDescending(x => x.UploadedAt).Select(x => new DispatchAttachmentDto
                {
                    Id = x.AttachmentId,
                    Name = x.Name,
                    SizeKb = x.SizeKb,
                    UploadedBy = x.UploadedBy,
                    UploadedAt = x.UploadedAt.ToString("o"),
                    Kind = x.Kind
                }).ToList(),
                Timeline = a.Timeline.OrderBy(x => x.Date).Select(x => new DispatchTimelineEventDto
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

        private static TransportDto MapTransportToDto(TransportDetail t)
        {
            return new TransportDto
            {
                Id = t.Id,
                TransportNumber = t.TransportNumber,
                DispatchId = t.DispatchId,
                DispatchNumber = t.DispatchNumber,
                VehicleAssignmentId = t.VehicleAssignmentId,
                AssignmentNumber = t.AssignmentNumber,
                VehicleNumber = t.VehicleNumber,
                DriverName = t.DriverName,
                TransportCompanyName = t.TransportCompanyName,
                Mode = t.Mode,
                Route = t.Route,
                Source = t.Source,
                Destination = t.Destination,
                DistanceKm = t.DistanceKm,
                EstimatedTimeHours = t.EstimatedTimeHours,
                ActualDeparture = t.ActualDeparture?.ToString("o"),
                ActualArrival = t.ActualArrival?.ToString("o"),
                FuelNotes = t.FuelNotes,
                Remarks = t.Remarks,
                Notes = t.Notes,
                Status = t.Status,
                LrId = t.LrId,
                CreatedBy = t.CreatedBy,
                CreatedAt = t.CreatedAt.ToString("o"),
                UpdatedBy = t.UpdatedBy,
                UpdatedAt = t.UpdatedAt.ToString("o")
            };
        }
    }
}
