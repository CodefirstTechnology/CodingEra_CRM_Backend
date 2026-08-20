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
    public class EwayBillService : IEwayBillService
    {
        private readonly ERPDbContext _db;
        private readonly EwayNumberingService _numbering;

        public EwayBillService(ERPDbContext db, EwayNumberingService numbering)
        {
            _db = db;
            _numbering = numbering;
        }

        public async Task<List<EwayListItemDto>> GetEwayBillsAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            var q = _db.EwayBills.AsNoTracking().AsQueryable();

            if (query != null)
            {
                if (!string.IsNullOrWhiteSpace(query.Search))
                {
                    var s = query.Search.Trim().ToLower();
                    q = q.Where(x => x.EwayBillNumber.ToLower().Contains(s) ||
                                     x.DispatchNumber.ToLower().Contains(s) ||
                                     x.InvoiceNumber.ToLower().Contains(s) ||
                                     x.CustomerName.ToLower().Contains(s) ||
                                     x.GstNumber.ToLower().Contains(s) ||
                                     x.VehicleNumber.ToLower().Contains(s));
                }

                if (!string.IsNullOrWhiteSpace(query.Status))
                {
                    var statusStr = query.Status.Trim();
                    if (Enum.TryParse<EwayStatus>(statusStr.Replace(" ", ""), true, out var st))
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
                    q = q.Where(x => x.ValidityFrom >= DateTime.SpecifyKind(df.Date, DateTimeKind.Utc));
                }

                if (!string.IsNullOrWhiteSpace(query.DateTo) && DateTime.TryParse(query.DateTo, out var dt))
                {
                    q = q.Where(x => x.ValidityTo <= DateTime.SpecifyKind(dt.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc));
                }
            }

            var list = await q.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

            return list.Select(x => new EwayListItemDto
            {
                Id = x.Id,
                EwayBillNumber = x.EwayBillNumber,
                DispatchNumber = x.DispatchNumber,
                InvoiceNumber = x.InvoiceNumber,
                CustomerName = x.CustomerName,
                GstNumber = x.GstNumber,
                VehicleNumber = x.VehicleNumber,
                ValidityTo = x.ValidityTo.ToString("yyyy-MM-dd"),
                Status = x.Status
            }).ToList();
        }

        public async Task<EwayDto> GetEwayBillByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var eway = await _db.EwayBills
                .AsNoTracking()
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (eway == null)
                throw new KeyNotFoundException($"E-Way bill with ID {id} not found.");

            return MapToDto(eway);
        }

        public async Task<EwayDto> CreateEwayBillAsync(EwayCreateRequestDto payload, string currentUser, CancellationToken cancellationToken = default)
        {
            ValidatePayload(payload);

            var validFrom = ParseDate(payload.ValidityFrom);
            var validTo = ParseDate(payload.ValidityTo);

            if (validFrom > validTo)
                throw new ArgumentException("Validity From must be on or before Validity To.");

            if (payload.DispatchId > 0)
            {
                var existing = await _db.EwayBills.FirstOrDefaultAsync(
                    x => x.DispatchId == payload.DispatchId && x.Status != EwayStatus.Closed, cancellationToken);
                if (existing != null)
                {
                    throw new InvalidOperationException($"E-Way bill {existing.EwayBillNumber} already exists for dispatch {payload.DispatchNumber}");
                }
            }

            var ewayNumber = await _numbering.NextNumberAsync(cancellationToken);
            var user = string.IsNullOrWhiteSpace(currentUser) ? "System" : currentUser;
            var now = DateTime.UtcNow;

            var eway = new EwayBill
            {
                EwayBillNumber = ewayNumber,
                DispatchId = payload.DispatchId,
                DispatchNumber = payload.DispatchNumber.Trim(),
                InvoiceNumber = payload.InvoiceNumber.Trim(),
                CustomerId = payload.CustomerId,
                CustomerName = payload.CustomerName.Trim(),
                GstNumber = payload.GstNumber.Trim(),
                VehicleNumber = payload.VehicleNumber.Trim(),
                TransportId = payload.TransportId,
                TransportNumber = payload.TransportNumber.Trim(),
                ValidityFrom = validFrom,
                ValidityTo = validTo,
                DistanceKm = payload.DistanceKm,
                TotalValue = 50000m,
                Remarks = payload.Remarks?.Trim() ?? string.Empty,
                Notes = payload.Notes?.Trim() ?? string.Empty,
                Status = EwayStatus.Draft,
                CreatedBy = user,
                CreatedAt = now,
                UpdatedBy = user,
                UpdatedAt = now
            };

            eway.Timeline.Add(new EwayTimelineEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Date = now,
                User = user,
                Action = "E-Way bill created",
                ToStatus = "Draft",
                Remarks = "Initial creation"
            });

            _db.EwayBills.Add(eway);
            await _db.SaveChangesAsync(cancellationToken);

            return MapToDto(eway);
        }

        public async Task<EwayDto> UpdateEwayBillAsync(int id, EwayUpdateRequestDto payload, string currentUser, CancellationToken cancellationToken = default)
        {
            ValidatePayload(payload);

            var validFrom = ParseDate(payload.ValidityFrom);
            var validTo = ParseDate(payload.ValidityTo);

            if (validFrom > validTo)
                throw new ArgumentException("Validity From must be on or before Validity To.");

            var eway = await _db.EwayBills
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (eway == null)
                throw new KeyNotFoundException($"E-Way bill with ID {id} not found.");

            if (eway.Status != EwayStatus.Draft)
                throw new InvalidOperationException("Only draft E-Way bills can be edited.");

            var user = string.IsNullOrWhiteSpace(currentUser) ? "System" : currentUser;
            var now = DateTime.UtcNow;

            eway.DispatchId = payload.DispatchId;
            eway.DispatchNumber = payload.DispatchNumber.Trim();
            eway.InvoiceNumber = payload.InvoiceNumber.Trim();
            eway.CustomerId = payload.CustomerId;
            eway.CustomerName = payload.CustomerName.Trim();
            eway.GstNumber = payload.GstNumber.Trim();
            eway.VehicleNumber = payload.VehicleNumber.Trim();
            eway.TransportId = payload.TransportId;
            eway.TransportNumber = payload.TransportNumber.Trim();
            eway.ValidityFrom = validFrom;
            eway.ValidityTo = validTo;
            eway.DistanceKm = payload.DistanceKm;
            eway.Remarks = payload.Remarks?.Trim() ?? string.Empty;
            eway.Notes = payload.Notes?.Trim() ?? string.Empty;
            eway.UpdatedBy = user;
            eway.UpdatedAt = now;

            eway.Timeline.Add(new EwayTimelineEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Date = now,
                User = user,
                Action = "E-Way bill updated",
                Remarks = "Draft updated"
            });

            await _db.SaveChangesAsync(cancellationToken);
            return MapToDto(eway);
        }

        public async Task DeleteEwayBillAsync(int id, CancellationToken cancellationToken = default)
        {
            var eway = await _db.EwayBills.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (eway == null)
                throw new KeyNotFoundException($"E-Way bill with ID {id} not found.");

            if (eway.Status != EwayStatus.Draft)
                throw new InvalidOperationException("Only draft E-Way bills can be deleted.");

            _db.EwayBills.Remove(eway);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<EwayDto> GenerateEwayBillAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default)
        {
            return await TransitionStatusAsync(
                id,
                new[] { EwayStatus.Draft },
                EwayStatus.Generated,
                "E-Way bill generated",
                payload,
                currentUser,
                cancellationToken);
        }

        public async Task<EwayDto> ActivateEwayBillAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default)
        {
            return await TransitionStatusAsync(
                id,
                new[] { EwayStatus.Generated },
                EwayStatus.Active,
                "E-Way bill activated",
                payload,
                currentUser,
                cancellationToken);
        }

        public async Task<EwayDto> ExtendValidityAsync(int id, EwayExtendRequestDto? payload, string currentUser, CancellationToken cancellationToken = default)
        {
            var eway = await _db.EwayBills
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (eway == null)
                throw new KeyNotFoundException($"E-Way bill with ID {id} not found.");

            if (eway.Status != EwayStatus.Active && eway.Status != EwayStatus.Expired)
                throw new InvalidOperationException("Only active or expired E-Way bills can have validity extended.");

            var newValidityTo = !string.IsNullOrWhiteSpace(payload?.ValidityTo)
                ? ParseDate(payload.ValidityTo)
                : eway.ValidityTo.AddDays(7);

            var user = string.IsNullOrWhiteSpace(currentUser) ? "System" : currentUser;
            var now = DateTime.UtcNow;
            var prevStatus = eway.Status.ToString();

            eway.ValidityTo = newValidityTo;
            eway.Status = EwayStatus.Active;
            eway.UpdatedBy = user;
            eway.UpdatedAt = now;

            eway.Timeline.Add(new EwayTimelineEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Date = now,
                User = user,
                Action = "Validity extended",
                FromStatus = prevStatus,
                ToStatus = EwayStatus.Active.ToString(),
                Remarks = payload?.Remarks?.Trim() ?? $"Extended to {newValidityTo:yyyy-MM-dd}"
            });

            await _db.SaveChangesAsync(cancellationToken);
            return MapToDto(eway);
        }

        public async Task<EwayDto> CloseEwayBillAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default)
        {
            return await TransitionStatusAsync(
                id,
                new[] { EwayStatus.Active, EwayStatus.Generated, EwayStatus.Expired },
                EwayStatus.Closed,
                "E-Way bill closed",
                payload,
                currentUser,
                cancellationToken);
        }

        public async Task<EwayDashboardDto> GetEwayDashboardAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.Date;
            var soon = today.AddDays(2).AddDays(1).AddTicks(-1);

            var activeBills = await _db.EwayBills
                .CountAsync(x => x.Status == EwayStatus.Active, cancellationToken);

            var expiringSoon = await _db.EwayBills
                .CountAsync(x => x.Status == EwayStatus.Active && x.ValidityTo >= today && x.ValidityTo <= soon, cancellationToken);

            var expired = await _db.EwayBills
                .CountAsync(x => x.Status == EwayStatus.Expired, cancellationToken);

            return new EwayDashboardDto
            {
                ActiveBills = activeBills,
                ExpiringSoon = expiringSoon,
                Expired = expired
            };
        }

        private async Task<EwayDto> TransitionStatusAsync(
            int id,
            EwayStatus[] allowedFromStatuses,
            EwayStatus toStatus,
            string defaultAction,
            StatusActionRequestDto? payload,
            string currentUser,
            CancellationToken cancellationToken)
        {
            var eway = await _db.EwayBills
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (eway == null)
                throw new KeyNotFoundException($"E-Way bill with ID {id} not found.");

            if (!allowedFromStatuses.Contains(eway.Status))
            {
                var allowed = string.Join(", ", allowedFromStatuses);
                throw new InvalidOperationException($"Invalid E-Way transition from {eway.Status} to {toStatus}. Allowed current status: [{allowed}].");
            }

            var user = !string.IsNullOrWhiteSpace(payload?.ApprovedBy)
                ? payload.ApprovedBy.Trim()
                : !string.IsNullOrWhiteSpace(currentUser)
                    ? currentUser
                    : "System";

            var now = DateTime.UtcNow;
            var fromStatusStr = eway.Status.ToString();
            var toStatusStr = toStatus.ToString();

            eway.Status = toStatus;
            eway.UpdatedBy = user;
            eway.UpdatedAt = now;

            eway.Timeline.Add(new EwayTimelineEvent
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
            return MapToDto(eway);
        }

        private static void ValidatePayload(EwayCreateRequestDto payload)
        {
            if (string.IsNullOrWhiteSpace(payload.DispatchNumber))
                throw new ArgumentException("Dispatch number is required.");
            if (string.IsNullOrWhiteSpace(payload.InvoiceNumber))
                throw new ArgumentException("Invoice number is required.");
            if (string.IsNullOrWhiteSpace(payload.CustomerName))
                throw new ArgumentException("Customer name is required.");
            if (string.IsNullOrWhiteSpace(payload.GstNumber))
                throw new ArgumentException("GST number is required.");
            if (string.IsNullOrWhiteSpace(payload.VehicleNumber))
                throw new ArgumentException("Vehicle number is required.");
            if (string.IsNullOrWhiteSpace(payload.TransportNumber))
                throw new ArgumentException("Transport number is required.");
            if (string.IsNullOrWhiteSpace(payload.ValidityFrom))
                throw new ArgumentException("Validity From is required.");
            if (string.IsNullOrWhiteSpace(payload.ValidityTo))
                throw new ArgumentException("Validity To is required.");

            if (payload.DistanceKm <= 0)
                throw new ArgumentException("Distance must be greater than 0.");
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

        private static EwayDto MapToDto(EwayBill e)
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
                UpdatedAt = e.UpdatedAt.ToString("o"),
                Attachments = e.Attachments.OrderByDescending(x => x.UploadedAt).Select(x => new DispatchAttachmentDto
                {
                    Id = x.AttachmentId,
                    Name = x.Name,
                    SizeKb = x.SizeKb,
                    UploadedBy = x.UploadedBy,
                    UploadedAt = x.UploadedAt.ToString("o"),
                    Kind = x.Kind
                }).ToList(),
                Timeline = e.Timeline.OrderBy(x => x.Date).Select(x => new DispatchTimelineEventDto
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
