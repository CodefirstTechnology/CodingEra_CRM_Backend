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
    public class LrManagementService : ILrManagementService
    {
        private readonly ERPDbContext _db;
        private readonly LrNumberingService _numbering;
        private readonly EwayNumberingService _ewayNumbering;

        public LrManagementService(
            ERPDbContext db,
            LrNumberingService numbering,
            EwayNumberingService ewayNumbering)
        {
            _db = db;
            _numbering = numbering;
            _ewayNumbering = ewayNumbering;
        }

        public async Task<List<LrListItemDto>> GetLrsAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            var q = _db.LorryReceipts.AsNoTracking().AsQueryable();

            if (query != null)
            {
                if (!string.IsNullOrWhiteSpace(query.Search))
                {
                    var s = query.Search.Trim().ToLower();
                    q = q.Where(x => x.LrNumber.ToLower().Contains(s) ||
                                     x.DispatchNumber.ToLower().Contains(s) ||
                                     x.TransportNumber.ToLower().Contains(s) ||
                                     x.VehicleNumber.ToLower().Contains(s) ||
                                     x.CustomerName.ToLower().Contains(s));
                }

                if (!string.IsNullOrWhiteSpace(query.Status))
                {
                    var statusStr = query.Status.Trim();
                    if (Enum.TryParse<LrStatus>(statusStr.Replace(" ", ""), true, out var st))
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
                    q = q.Where(x => x.LrDate >= DateTime.SpecifyKind(df.Date, DateTimeKind.Utc));
                }

                if (!string.IsNullOrWhiteSpace(query.DateTo) && DateTime.TryParse(query.DateTo, out var dt))
                {
                    q = q.Where(x => x.LrDate <= DateTime.SpecifyKind(dt.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc));
                }
            }

            var list = await q.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

            return list.Select(x => new LrListItemDto
            {
                Id = x.Id,
                LrNumber = x.LrNumber,
                DispatchNumber = x.DispatchNumber,
                TransportNumber = x.TransportNumber,
                VehicleNumber = x.VehicleNumber,
                CustomerName = x.CustomerName,
                LrDate = x.LrDate.ToString("yyyy-MM-dd"),
                Packages = x.Packages,
                WeightKg = x.WeightKg,
                FreightCharges = x.FreightCharges,
                Status = x.Status
            }).ToList();
        }

        public async Task<LrDto> GetLrByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var lr = await _db.LorryReceipts
                .AsNoTracking()
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (lr == null)
                throw new KeyNotFoundException($"Lorry receipt with ID {id} not found.");

            return MapToDto(lr);
        }

        public async Task<LrDto> CreateLrAsync(LrCreateRequestDto payload, string currentUser, CancellationToken cancellationToken = default)
        {
            ValidatePayload(payload);

            var lrDate = ParseDate(payload.LrDate);
            var lrNumber = await _numbering.NextNumberAsync(cancellationToken);
            var user = string.IsNullOrWhiteSpace(currentUser) ? "System" : currentUser;
            var now = DateTime.UtcNow;

            var lr = new LorryReceipt
            {
                LrNumber = lrNumber,
                DispatchId = payload.DispatchId,
                DispatchNumber = payload.DispatchNumber.Trim(),
                TransportId = payload.TransportId,
                TransportNumber = payload.TransportNumber.Trim(),
                VehicleNumber = payload.VehicleNumber.Trim(),
                CustomerId = payload.CustomerId,
                CustomerName = payload.CustomerName.Trim(),
                LrDate = lrDate,
                Consignor = payload.Consignor.Trim(),
                Consignee = payload.Consignee.Trim(),
                Packages = payload.Packages,
                WeightKg = payload.WeightKg,
                FreightCharges = payload.FreightCharges,
                PaymentType = payload.PaymentType,
                Remarks = payload.Remarks?.Trim() ?? string.Empty,
                Notes = payload.Notes?.Trim() ?? string.Empty,
                Status = LrStatus.Draft,
                CreatedBy = user,
                CreatedAt = now,
                UpdatedBy = user,
                UpdatedAt = now
            };

            lr.Timeline.Add(new LrTimelineEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Date = now,
                User = user,
                Action = "LR created",
                ToStatus = "Draft",
                Remarks = "Initial creation"
            });

            _db.LorryReceipts.Add(lr);
            await _db.SaveChangesAsync(cancellationToken);

            if (payload.TransportId > 0)
            {
                var transport = await _db.TransportDetails.FirstOrDefaultAsync(x => x.Id == payload.TransportId, cancellationToken);
                if (transport != null)
                {
                    transport.LrId = lr.Id;
                    transport.UpdatedAt = now;
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }

            return MapToDto(lr);
        }

        public async Task<LrDto> UpdateLrAsync(int id, LrUpdateRequestDto payload, string currentUser, CancellationToken cancellationToken = default)
        {
            ValidatePayload(payload);

            var lr = await _db.LorryReceipts
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (lr == null)
                throw new KeyNotFoundException($"Lorry receipt with ID {id} not found.");

            if (lr.Status != LrStatus.Draft)
                throw new InvalidOperationException("Only draft LRs can be edited.");

            var lrDate = ParseDate(payload.LrDate);
            var user = string.IsNullOrWhiteSpace(currentUser) ? "System" : currentUser;
            var now = DateTime.UtcNow;

            lr.DispatchId = payload.DispatchId;
            lr.DispatchNumber = payload.DispatchNumber.Trim();
            lr.TransportId = payload.TransportId;
            lr.TransportNumber = payload.TransportNumber.Trim();
            lr.VehicleNumber = payload.VehicleNumber.Trim();
            lr.CustomerId = payload.CustomerId;
            lr.CustomerName = payload.CustomerName.Trim();
            lr.LrDate = lrDate;
            lr.Consignor = payload.Consignor.Trim();
            lr.Consignee = payload.Consignee.Trim();
            lr.Packages = payload.Packages;
            lr.WeightKg = payload.WeightKg;
            lr.FreightCharges = payload.FreightCharges;
            lr.PaymentType = payload.PaymentType;
            lr.Remarks = payload.Remarks?.Trim() ?? string.Empty;
            lr.Notes = payload.Notes?.Trim() ?? string.Empty;
            lr.UpdatedBy = user;
            lr.UpdatedAt = now;

            lr.Timeline.Add(new LrTimelineEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Date = now,
                User = user,
                Action = "LR updated",
                Remarks = "Draft updated"
            });

            await _db.SaveChangesAsync(cancellationToken);
            return MapToDto(lr);
        }

        public async Task DeleteLrAsync(int id, CancellationToken cancellationToken = default)
        {
            var lr = await _db.LorryReceipts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (lr == null)
                throw new KeyNotFoundException($"Lorry receipt with ID {id} not found.");

            if (lr.Status != LrStatus.Draft)
                throw new InvalidOperationException("Only draft LRs can be deleted.");

            _db.LorryReceipts.Remove(lr);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<LrDto> GenerateLrDocumentAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default)
        {
            return await TransitionStatusAsync(
                id,
                new[] { LrStatus.Draft },
                LrStatus.Generated,
                "LR document generated",
                payload,
                currentUser,
                cancellationToken);
        }

        public async Task<LrDto> IssueLrAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default)
        {
            return await TransitionStatusAsync(
                id,
                new[] { LrStatus.Generated },
                LrStatus.Issued,
                "LR issued",
                payload,
                currentUser,
                cancellationToken);
        }

        public async Task<LrDto> CloseLrAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default)
        {
            return await TransitionStatusAsync(
                id,
                new[] { LrStatus.Issued },
                LrStatus.Closed,
                "LR closed",
                payload,
                currentUser,
                cancellationToken);
        }

        public async Task<EwayDto> GenerateEwayFromLrAsync(int lrId, string currentUser, CancellationToken cancellationToken = default)
        {
            var lr = await _db.LorryReceipts.FirstOrDefaultAsync(x => x.Id == lrId, cancellationToken);
            if (lr == null)
                throw new KeyNotFoundException($"Lorry receipt with ID {lrId} not found.");

            if (lr.Status != LrStatus.Issued && lr.Status != LrStatus.Generated && lr.Status != LrStatus.Closed)
                throw new InvalidOperationException("LR must be Generated or Issued before creating E-Way bill.");

            var existing = await _db.EwayBills.FirstOrDefaultAsync(x => x.DispatchId == lr.DispatchId && x.Status != EwayStatus.Closed, cancellationToken);
            if (existing != null)
            {
                return MapEwayToDto(existing);
            }

            var transport = await _db.TransportDetails.FirstOrDefaultAsync(x => x.Id == lr.TransportId, cancellationToken);
            var ewayNumber = await _ewayNumbering.NextNumberAsync(cancellationToken);
            var user = string.IsNullOrWhiteSpace(currentUser) ? "System" : currentUser;
            var now = DateTime.UtcNow;
            var validityTo = now.AddDays(2);

            var eway = new EwayBill
            {
                EwayBillNumber = ewayNumber,
                DispatchId = lr.DispatchId,
                DispatchNumber = lr.DispatchNumber,
                InvoiceNumber = $"INV-{now.Year}-{lr.DispatchId + 100:D4}",
                CustomerId = lr.CustomerId,
                CustomerName = lr.CustomerName,
                GstNumber = "27AABCA1234A1Z5",
                VehicleNumber = lr.VehicleNumber,
                TransportId = lr.TransportId > 0 ? lr.TransportId : transport?.Id ?? 0,
                TransportNumber = !string.IsNullOrWhiteSpace(lr.TransportNumber) ? lr.TransportNumber : transport?.TransportNumber ?? "—",
                ValidityFrom = now,
                ValidityTo = validityTo,
                DistanceKm = transport?.DistanceKm ?? 100m,
                TotalValue = 50000m,
                Remarks = $"Generated from LR {lr.LrNumber}",
                Notes = string.Empty,
                Status = EwayStatus.Generated,
                CreatedBy = user,
                CreatedAt = now,
                UpdatedBy = user,
                UpdatedAt = now
            };

            _db.EwayBills.Add(eway);
            await _db.SaveChangesAsync(cancellationToken);

            return MapEwayToDto(eway);
        }

        public async Task<LrDashboardDto> GetLrDashboardAsync(CancellationToken cancellationToken = default)
        {
            var generated = await _db.LorryReceipts
                .CountAsync(x => x.Status == LrStatus.Generated || x.Status == LrStatus.Issued || x.Status == LrStatus.Closed, cancellationToken);

            var issued = await _db.LorryReceipts
                .CountAsync(x => x.Status == LrStatus.Issued, cancellationToken);

            var pending = await _db.LorryReceipts
                .CountAsync(x => x.Status == LrStatus.Draft, cancellationToken);

            return new LrDashboardDto
            {
                Generated = generated,
                Issued = issued,
                Pending = pending
            };
        }

        private async Task<LrDto> TransitionStatusAsync(
            int id,
            LrStatus[] allowedFromStatuses,
            LrStatus toStatus,
            string defaultAction,
            StatusActionRequestDto? payload,
            string currentUser,
            CancellationToken cancellationToken)
        {
            var lr = await _db.LorryReceipts
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (lr == null)
                throw new KeyNotFoundException($"Lorry receipt with ID {id} not found.");

            if (!allowedFromStatuses.Contains(lr.Status))
            {
                var allowed = string.Join(", ", allowedFromStatuses);
                throw new InvalidOperationException($"Invalid LR transition from {lr.Status} to {toStatus}. Allowed current status: [{allowed}].");
            }

            var user = !string.IsNullOrWhiteSpace(payload?.ApprovedBy)
                ? payload.ApprovedBy.Trim()
                : !string.IsNullOrWhiteSpace(currentUser)
                    ? currentUser
                    : "System";

            var now = DateTime.UtcNow;
            var fromStatusStr = lr.Status.ToString();
            var toStatusStr = toStatus.ToString();

            lr.Status = toStatus;
            lr.UpdatedBy = user;
            lr.UpdatedAt = now;

            lr.Timeline.Add(new LrTimelineEvent
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
            return MapToDto(lr);
        }

        private static void ValidatePayload(LrCreateRequestDto payload)
        {
            if (string.IsNullOrWhiteSpace(payload.DispatchNumber))
                throw new ArgumentException("Dispatch number is required.");
            if (string.IsNullOrWhiteSpace(payload.TransportNumber))
                throw new ArgumentException("Transport number is required.");
            if (string.IsNullOrWhiteSpace(payload.VehicleNumber))
                throw new ArgumentException("Vehicle number is required.");
            if (string.IsNullOrWhiteSpace(payload.CustomerName))
                throw new ArgumentException("Customer name is required.");
            if (string.IsNullOrWhiteSpace(payload.LrDate))
                throw new ArgumentException("LR date is required.");
            if (string.IsNullOrWhiteSpace(payload.Consignor))
                throw new ArgumentException("Consignor is required.");
            if (string.IsNullOrWhiteSpace(payload.Consignee))
                throw new ArgumentException("Consignee is required.");

            if (payload.Packages <= 0)
                throw new ArgumentException("Packages must be greater than 0.");
            if (payload.WeightKg <= 0)
                throw new ArgumentException("Weight must be greater than 0.");
            if (payload.FreightCharges < 0)
                throw new ArgumentException("Freight charges cannot be negative.");
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

        private static LrDto MapToDto(LorryReceipt r)
        {
            return new LrDto
            {
                Id = r.Id,
                LrNumber = r.LrNumber,
                DispatchId = r.DispatchId,
                DispatchNumber = r.DispatchNumber,
                TransportId = r.TransportId,
                TransportNumber = r.TransportNumber,
                VehicleNumber = r.VehicleNumber,
                CustomerId = r.CustomerId,
                CustomerName = r.CustomerName,
                LrDate = r.LrDate.ToString("yyyy-MM-dd"),
                Consignor = r.Consignor,
                Consignee = r.Consignee,
                Packages = r.Packages,
                WeightKg = r.WeightKg,
                FreightCharges = r.FreightCharges,
                PaymentType = r.PaymentType,
                Remarks = r.Remarks,
                Notes = r.Notes,
                Status = r.Status,
                CreatedBy = r.CreatedBy,
                CreatedAt = r.CreatedAt.ToString("o"),
                UpdatedBy = r.UpdatedBy,
                UpdatedAt = r.UpdatedAt.ToString("o"),
                Attachments = r.Attachments.OrderByDescending(x => x.UploadedAt).Select(x => new DispatchAttachmentDto
                {
                    Id = x.AttachmentId,
                    Name = x.Name,
                    SizeKb = x.SizeKb,
                    UploadedBy = x.UploadedBy,
                    UploadedAt = x.UploadedAt.ToString("o"),
                    Kind = x.Kind
                }).ToList(),
                Timeline = r.Timeline.OrderBy(x => x.Date).Select(x => new DispatchTimelineEventDto
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

        private static EwayDto MapEwayToDto(EwayBill e)
        {
            return new EwayDto
            {
                Id = e.Id,
                EwayBillNumber = e.EwayBillNumber,
                DispatchId = e.DispatchId,
                DispatchNumber = e.DispatchNumber,
                InvoiceNumber = e.InvoiceNumber,
                CustomerId = e.CustomerId,
                CustomerName = e.CustomerName,
                GstNumber = e.GstNumber,
                VehicleNumber = e.VehicleNumber,
                TransportId = e.TransportId,
                TransportNumber = e.TransportNumber,
                ValidityFrom = e.ValidityFrom.ToString("yyyy-MM-dd"),
                ValidityTo = e.ValidityTo.ToString("yyyy-MM-dd"),
                DistanceKm = e.DistanceKm,
                TotalValue = e.TotalValue,
                Remarks = e.Remarks,
                Notes = e.Notes,
                Status = e.Status,
                CreatedBy = e.CreatedBy,
                CreatedAt = e.CreatedAt.ToString("o"),
                UpdatedBy = e.UpdatedBy,
                UpdatedAt = e.UpdatedAt.ToString("o")
            };
        }
    }
}
