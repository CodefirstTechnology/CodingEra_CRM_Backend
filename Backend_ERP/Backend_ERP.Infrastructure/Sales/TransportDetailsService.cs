using System;
using System.Collections.Generic;
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
    public class TransportDetailsService : ITransportDetailsService
    {
        private readonly ERPDbContext _db;
        private readonly TransportNumberingService _numbering;
        private readonly LrNumberingService _lrNumbering;

        public TransportDetailsService(
            ERPDbContext db,
            TransportNumberingService numbering,
            LrNumberingService lrNumbering)
        {
            _db = db;
            _numbering = numbering;
            _lrNumbering = lrNumbering;
        }

        public async Task<List<TransportListItemDto>> GetTransportsAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            var q = _db.TransportDetails.AsNoTracking().AsQueryable();

            if (query != null)
            {
                if (!string.IsNullOrWhiteSpace(query.Search))
                {
                    var s = query.Search.Trim().ToLower();
                    q = q.Where(x => x.TransportNumber.ToLower().Contains(s) ||
                                     x.DispatchNumber.ToLower().Contains(s) ||
                                     x.VehicleNumber.ToLower().Contains(s) ||
                                     x.DriverName.ToLower().Contains(s) ||
                                     x.TransportCompanyName.ToLower().Contains(s) ||
                                     x.Route.ToLower().Contains(s));
                }

                if (!string.IsNullOrWhiteSpace(query.Status))
                {
                    var statusStr = query.Status.Trim();
                    if (string.Equals(statusStr, "In Transit", StringComparison.OrdinalIgnoreCase))
                    {
                        q = q.Where(x => x.Status == TransportStatus.InTransit);
                    }
                    else if (Enum.TryParse<TransportStatus>(statusStr.Replace(" ", ""), true, out var st))
                    {
                        q = q.Where(x => x.Status == st);
                    }
                }

                if (query.DispatchId.HasValue && query.DispatchId.Value > 0)
                {
                    q = q.Where(x => x.DispatchId == query.DispatchId.Value);
                }
            }

            var list = await q.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

            return list.Select(x => new TransportListItemDto
            {
                Id = x.Id,
                TransportNumber = x.TransportNumber,
                DispatchNumber = x.DispatchNumber,
                VehicleNumber = x.VehicleNumber,
                DriverName = x.DriverName,
                TransportCompanyName = x.TransportCompanyName,
                Mode = x.Mode,
                Route = x.Route,
                DistanceKm = x.DistanceKm,
                EstimatedTimeHours = x.EstimatedTimeHours,
                Status = x.Status
            }).ToList();
        }

        public async Task<TransportDto> GetTransportByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var transport = await _db.TransportDetails
                .AsNoTracking()
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (transport == null)
                throw new KeyNotFoundException($"Transport details with ID {id} not found.");

            return MapToDto(transport);
        }

        public async Task<TransportDto> CreateTransportAsync(TransportCreateRequestDto payload, string currentUser, CancellationToken cancellationToken = default)
        {
            ValidatePayload(payload);

            var transportNumber = await _numbering.NextNumberAsync(cancellationToken);
            var user = string.IsNullOrWhiteSpace(currentUser) ? "System" : currentUser;
            var now = DateTime.UtcNow;

            var transport = new TransportDetail
            {
                TransportNumber = transportNumber,
                DispatchId = payload.DispatchId,
                DispatchNumber = payload.DispatchNumber.Trim(),
                VehicleAssignmentId = payload.VehicleAssignmentId,
                AssignmentNumber = payload.AssignmentNumber.Trim(),
                VehicleNumber = payload.VehicleNumber.Trim(),
                DriverName = payload.DriverName.Trim(),
                TransportCompanyName = payload.TransportCompanyName.Trim(),
                Mode = payload.Mode,
                Route = payload.Route.Trim(),
                Source = payload.Source.Trim(),
                Destination = payload.Destination.Trim(),
                DistanceKm = payload.DistanceKm,
                EstimatedTimeHours = payload.EstimatedTimeHours,
                FuelNotes = payload.FuelNotes?.Trim() ?? string.Empty,
                Remarks = payload.Remarks?.Trim() ?? string.Empty,
                Notes = payload.Notes?.Trim() ?? string.Empty,
                Status = TransportStatus.Draft,
                CreatedBy = user,
                CreatedAt = now,
                UpdatedBy = user,
                UpdatedAt = now
            };

            transport.Timeline.Add(new TransportTimelineEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Date = now,
                User = user,
                Action = "Transport created",
                ToStatus = "Draft",
                Remarks = "Initial creation"
            });

            _db.TransportDetails.Add(transport);
            await _db.SaveChangesAsync(cancellationToken);

            // Link to VehicleAssignment and DispatchPlan if present
            if (payload.VehicleAssignmentId > 0)
            {
                var va = await _db.VehicleAssignments.FirstOrDefaultAsync(x => x.Id == payload.VehicleAssignmentId, cancellationToken);
                if (va != null)
                {
                    va.TransportId = transport.Id;
                    va.UpdatedAt = now;
                }
            }

            if (payload.DispatchId > 0)
            {
                var plan = await _db.DispatchPlans.FirstOrDefaultAsync(x => x.Id == payload.DispatchId, cancellationToken);
                if (plan != null)
                {
                    plan.TransportId = transport.Id;
                    plan.UpdatedAt = now;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            return MapToDto(transport);
        }

        public async Task<TransportDto> UpdateTransportAsync(int id, TransportUpdateRequestDto payload, string currentUser, CancellationToken cancellationToken = default)
        {
            ValidatePayload(payload);

            var transport = await _db.TransportDetails
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (transport == null)
                throw new KeyNotFoundException($"Transport details with ID {id} not found.");

            if (transport.Status != TransportStatus.Draft)
                throw new InvalidOperationException("Only draft transports can be edited.");

            var user = string.IsNullOrWhiteSpace(currentUser) ? "System" : currentUser;
            var now = DateTime.UtcNow;

            transport.DispatchId = payload.DispatchId;
            transport.DispatchNumber = payload.DispatchNumber.Trim();
            transport.VehicleAssignmentId = payload.VehicleAssignmentId;
            transport.AssignmentNumber = payload.AssignmentNumber.Trim();
            transport.VehicleNumber = payload.VehicleNumber.Trim();
            transport.DriverName = payload.DriverName.Trim();
            transport.TransportCompanyName = payload.TransportCompanyName.Trim();
            transport.Mode = payload.Mode;
            transport.Route = payload.Route.Trim();
            transport.Source = payload.Source.Trim();
            transport.Destination = payload.Destination.Trim();
            transport.DistanceKm = payload.DistanceKm;
            transport.EstimatedTimeHours = payload.EstimatedTimeHours;
            transport.FuelNotes = payload.FuelNotes?.Trim() ?? string.Empty;
            transport.Remarks = payload.Remarks?.Trim() ?? string.Empty;
            transport.Notes = payload.Notes?.Trim() ?? string.Empty;
            transport.UpdatedBy = user;
            transport.UpdatedAt = now;

            transport.Timeline.Add(new TransportTimelineEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Date = now,
                User = user,
                Action = "Transport updated",
                Remarks = "Draft updated"
            });

            await _db.SaveChangesAsync(cancellationToken);
            return MapToDto(transport);
        }

        public async Task DeleteTransportAsync(int id, CancellationToken cancellationToken = default)
        {
            var transport = await _db.TransportDetails.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (transport == null)
                throw new KeyNotFoundException($"Transport details with ID {id} not found.");

            if (transport.Status != TransportStatus.Draft)
                throw new InvalidOperationException("Only draft transports can be deleted.");

            _db.TransportDetails.Remove(transport);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<TransportDto> StartJourneyAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default)
        {
            return await TransitionStatusAsync(
                id,
                new[] { TransportStatus.Draft },
                TransportStatus.InTransit,
                "Journey started",
                payload,
                currentUser,
                setDeparture: true,
                setArrival: false,
                cancellationToken: cancellationToken);
        }

        public async Task<TransportDto> MarkInTransitAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default)
        {
            return await TransitionStatusAsync(
                id,
                new[] { TransportStatus.Draft },
                TransportStatus.InTransit,
                "Marked in transit",
                payload,
                currentUser,
                setDeparture: true,
                setArrival: false,
                cancellationToken: cancellationToken);
        }

        public async Task<TransportDto> MarkDeliveredAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default)
        {
            return await TransitionStatusAsync(
                id,
                new[] { TransportStatus.InTransit },
                TransportStatus.Delivered,
                "Marked delivered",
                payload,
                currentUser,
                setDeparture: false,
                setArrival: true,
                cancellationToken: cancellationToken);
        }

        public async Task<TransportDto> CloseTransportAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(payload?.Remarks))
                throw new ArgumentException("Close remarks are required.");

            return await TransitionStatusAsync(
                id,
                new[] { TransportStatus.Delivered },
                TransportStatus.Closed,
                "Transport closed",
                payload,
                currentUser,
                setDeparture: false,
                setArrival: false,
                cancellationToken: cancellationToken);
        }

        public async Task<LrDto> GenerateLrAsync(int id, string currentUser, CancellationToken cancellationToken = default)
        {
            var transport = await _db.TransportDetails
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (transport == null)
                throw new KeyNotFoundException($"Transport details with ID {id} not found.");

            if (transport.LrId.HasValue && transport.LrId.Value > 0)
            {
                var existing = await _db.LorryReceipts.FirstOrDefaultAsync(x => x.Id == transport.LrId.Value, cancellationToken);
                if (existing != null)
                {
                    return MapLrToDto(existing);
                }
            }

            var plan = await _db.DispatchPlans
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == transport.DispatchId, cancellationToken);

            var lrNumber = await _lrNumbering.NextNumberAsync(cancellationToken);
            var user = string.IsNullOrWhiteSpace(currentUser) ? "System" : currentUser;
            var now = DateTime.UtcNow;

            var totalQty = plan?.Items.Sum(x => x.Quantity) ?? 1;
            var packages = plan?.Items.Count ?? 1;
            var weightKg = totalQty * 5; // Default standard estimation

            var lr = new LorryReceipt
            {
                LrNumber = lrNumber,
                DispatchId = transport.DispatchId,
                DispatchNumber = transport.DispatchNumber,
                TransportId = transport.Id,
                TransportNumber = transport.TransportNumber,
                VehicleNumber = transport.VehicleNumber,
                CustomerId = plan?.CustomerId ?? 0,
                CustomerName = plan?.CustomerName ?? "Customer",
                LrDate = now,
                Consignor = "CodingEra Manufacturing Pvt Ltd",
                Consignee = plan?.CustomerName ?? "Consignee",
                Packages = Math.Max(1, packages),
                WeightKg = weightKg,
                FreightCharges = 0,
                PaymentType = FreightPaymentType.ToBeBilled,
                Remarks = $"Generated from {transport.TransportNumber}",
                Notes = string.Empty,
                Status = LrStatus.Draft,
                CreatedBy = user,
                CreatedAt = now,
                UpdatedBy = user,
                UpdatedAt = now
            };

            _db.LorryReceipts.Add(lr);
            await _db.SaveChangesAsync(cancellationToken);

            transport.LrId = lr.Id;
            transport.UpdatedBy = user;
            transport.UpdatedAt = now;
            transport.Timeline.Add(new TransportTimelineEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Date = now,
                User = user,
                Action = $"LR {lr.LrNumber} generated",
                FromStatus = transport.Status == TransportStatus.InTransit ? "In Transit" : transport.Status.ToString(),
                ToStatus = transport.Status == TransportStatus.InTransit ? "In Transit" : transport.Status.ToString(),
                Remarks = $"LR ID {lr.Id}"
            });

            await _db.SaveChangesAsync(cancellationToken);
            return MapLrToDto(lr);
        }

        public async Task<TransportDashboardDto> GetTransportDashboardAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            var now = DateTime.UtcNow;

            var vehiclesInTransit = await _db.TransportDetails
                .CountAsync(x => x.Status == TransportStatus.InTransit, cancellationToken);

            var deliveredToday = await _db.TransportDetails
                .CountAsync(x => (x.Status == TransportStatus.Delivered || x.Status == TransportStatus.Closed) &&
                                 x.ActualArrival.HasValue &&
                                 x.ActualArrival.Value >= today &&
                                 x.ActualArrival.Value < tomorrow, cancellationToken);

            var inTransitList = await _db.TransportDetails
                .Where(x => x.Status == TransportStatus.InTransit && x.EstimatedTimeHours > 0 && x.ActualDeparture.HasValue)
                .ToListAsync(cancellationToken);

            var delayedShipments = inTransitList
                .Count(x => (now - x.ActualDeparture!.Value).TotalHours > (double)x.EstimatedTimeHours);

            return new TransportDashboardDto
            {
                VehiclesInTransit = vehiclesInTransit,
                DeliveredToday = deliveredToday,
                DelayedShipments = delayedShipments
            };
        }

        private async Task<TransportDto> TransitionStatusAsync(
            int id,
            TransportStatus[] allowedFromStatuses,
            TransportStatus toStatus,
            string defaultAction,
            StatusActionRequestDto? payload,
            string currentUser,
            bool setDeparture,
            bool setArrival,
            CancellationToken cancellationToken)
        {
            var transport = await _db.TransportDetails
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (transport == null)
                throw new KeyNotFoundException($"Transport details with ID {id} not found.");

            if (!allowedFromStatuses.Contains(transport.Status))
            {
                var allowed = string.Join(", ", allowedFromStatuses);
                throw new InvalidOperationException($"Invalid transport transition from {transport.Status} to {toStatus}. Allowed current status: [{allowed}].");
            }

            var user = !string.IsNullOrWhiteSpace(payload?.ApprovedBy)
                ? payload.ApprovedBy.Trim()
                : !string.IsNullOrWhiteSpace(currentUser)
                    ? currentUser
                    : "System";

            var now = DateTime.UtcNow;
            var fromStatusStr = transport.Status == TransportStatus.InTransit ? "In Transit" : transport.Status.ToString();
            var toStatusStr = toStatus == TransportStatus.InTransit ? "In Transit" : toStatus.ToString();

            transport.Status = toStatus;
            if (setDeparture && !transport.ActualDeparture.HasValue)
            {
                transport.ActualDeparture = now;
            }

            if (setArrival && !transport.ActualArrival.HasValue)
            {
                transport.ActualArrival = now;
            }

            transport.UpdatedBy = user;
            transport.UpdatedAt = now;

            transport.Timeline.Add(new TransportTimelineEvent
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
            return MapToDto(transport);
        }

        private static void ValidatePayload(TransportCreateRequestDto payload)
        {
            if (string.IsNullOrWhiteSpace(payload.DispatchNumber))
                throw new ArgumentException("Dispatch number is required.");
            if (string.IsNullOrWhiteSpace(payload.VehicleNumber))
                throw new ArgumentException("Vehicle number is required.");
            if (string.IsNullOrWhiteSpace(payload.DriverName))
                throw new ArgumentException("Driver name is required.");
            if (string.IsNullOrWhiteSpace(payload.TransportCompanyName))
                throw new ArgumentException("Transport company name is required.");
            if (string.IsNullOrWhiteSpace(payload.Route))
                throw new ArgumentException("Route is required.");
            if (string.IsNullOrWhiteSpace(payload.Source))
                throw new ArgumentException("Source is required.");
            if (string.IsNullOrWhiteSpace(payload.Destination))
                throw new ArgumentException("Destination is required.");

            if (payload.DistanceKm < 0)
                throw new ArgumentException("Distance cannot be negative.");
            if (payload.EstimatedTimeHours < 0)
                throw new ArgumentException("Estimated time cannot be negative.");
        }

        private static TransportDto MapToDto(TransportDetail t)
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
                UpdatedAt = t.UpdatedAt.ToString("o"),
                Attachments = t.Attachments.OrderByDescending(x => x.UploadedAt).Select(x => new DispatchAttachmentDto
                {
                    Id = x.AttachmentId,
                    Name = x.Name,
                    SizeKb = x.SizeKb,
                    UploadedBy = x.UploadedBy,
                    UploadedAt = x.UploadedAt.ToString("o"),
                    Kind = x.Kind
                }).ToList(),
                Timeline = t.Timeline.OrderBy(x => x.Date).Select(x => new DispatchTimelineEventDto
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

        private static LrDto MapLrToDto(LorryReceipt lr)
        {
            return new LrDto
            {
                Id = lr.Id,
                LrNumber = lr.LrNumber,
                DispatchId = lr.DispatchId,
                DispatchNumber = lr.DispatchNumber,
                TransportId = lr.TransportId,
                TransportNumber = lr.TransportNumber,
                VehicleNumber = lr.VehicleNumber,
                CustomerId = lr.CustomerId,
                CustomerName = lr.CustomerName,
                LrDate = lr.LrDate.ToString("yyyy-MM-dd"),
                Consignor = lr.Consignor,
                Consignee = lr.Consignee,
                Packages = lr.Packages,
                WeightKg = lr.WeightKg,
                FreightCharges = lr.FreightCharges,
                PaymentType = lr.PaymentType,
                Remarks = lr.Remarks,
                Notes = lr.Notes,
                Status = lr.Status,
                CreatedBy = lr.CreatedBy,
                CreatedAt = lr.CreatedAt.ToString("o"),
                UpdatedBy = lr.UpdatedBy,
                UpdatedAt = lr.UpdatedAt.ToString("o")
            };
        }
    }
}
