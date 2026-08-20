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
    public class PodService : IPodService
    {
        private readonly ERPDbContext _db;
        private readonly PodNumberingService _numbering;

        public PodService(ERPDbContext db, PodNumberingService numbering)
        {
            _db = db;
            _numbering = numbering;
        }

        public async Task<List<PodListItemDto>> GetPodsAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            var q = _db.DeliveryConfirmations.AsNoTracking().AsQueryable();

            if (query != null)
            {
                if (!string.IsNullOrWhiteSpace(query.Search))
                {
                    var s = query.Search.Trim().ToLower();
                    q = q.Where(x =>
                        x.PodNumber.ToLower().Contains(s) ||
                        x.DispatchNumber.ToLower().Contains(s) ||
                        x.CustomerName.ToLower().Contains(s) ||
                        x.ReceiverName.ToLower().Contains(s));
                }

                if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<PodStatus>(query.Status.Trim(), true, out var st))
                {
                    q = q.Where(x => x.Status == st);
                }

                if (query.DispatchId.HasValue && query.DispatchId.Value > 0)
                {
                    q = q.Where(x => x.DispatchId == query.DispatchId.Value);
                }

                if (!string.IsNullOrWhiteSpace(query.DateFrom) && DateTime.TryParse(query.DateFrom, out var df))
                {
                    q = q.Where(x => x.DeliveryDate >= DateTime.SpecifyKind(df.Date, DateTimeKind.Utc));
                }

                if (!string.IsNullOrWhiteSpace(query.DateTo) && DateTime.TryParse(query.DateTo, out var dt))
                {
                    q = q.Where(x => x.DeliveryDate <= DateTime.SpecifyKind(dt.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc));
                }
            }

            var list = await q.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

            return list.Select(x => new PodListItemDto
            {
                Id = x.Id,
                PodNumber = x.PodNumber,
                DispatchNumber = x.DispatchNumber,
                CustomerName = x.CustomerName,
                DeliveryDate = x.DeliveryDate.ToString("yyyy-MM-dd"),
                ReceiverName = x.ReceiverName,
                ReceiverContact = x.ReceiverContact,
                Status = x.Status
            }).ToList();
        }

        public async Task<PodDto> GetPodByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var pod = await _db.DeliveryConfirmations
                .AsNoTracking()
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (pod == null)
                throw new KeyNotFoundException($"POD with ID {id} not found.");

            return MapToDto(pod);
        }

        public async Task<PodDto> CreatePodAsync(PodCreateRequestDto payload, string currentUser, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(payload.ReceiverName) || string.IsNullOrWhiteSpace(payload.ReceiverContact))
                throw new ArgumentException("Receiver name and contact are required.");

            if (payload.DispatchId > 0)
            {
                var existing = await _db.DeliveryConfirmations
                    .FirstOrDefaultAsync(x => x.DispatchId == payload.DispatchId && x.Status != PodStatus.Closed, cancellationToken);

                if (existing != null)
                {
                    throw new InvalidOperationException($"POD {existing.PodNumber} already exists for dispatch {payload.DispatchNumber}");
                }
            }

            var podNumber = await _numbering.GenerateNextNumberAsync(cancellationToken);
            var now = DateTime.UtcNow;
            var user = string.IsNullOrWhiteSpace(currentUser) ? "System" : currentUser;

            var deliveryDate = DateTime.UtcNow.Date;
            if (!string.IsNullOrWhiteSpace(payload.DeliveryDate) &&
                DateTime.TryParse(payload.DeliveryDate, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dd))
            {
                deliveryDate = DateTime.SpecifyKind(dd.Date, DateTimeKind.Utc);
            }

            var pod = new DeliveryConfirmation
            {
                PodNumber = podNumber,
                DispatchId = payload.DispatchId,
                DispatchNumber = payload.DispatchNumber?.Trim() ?? string.Empty,
                CustomerId = payload.CustomerId,
                CustomerName = payload.CustomerName?.Trim() ?? string.Empty,
                DeliveryDate = deliveryDate,
                ReceiverName = payload.ReceiverName.Trim(),
                ReceiverContact = payload.ReceiverContact.Trim(),
                DeliveryRemarks = payload.DeliveryRemarks?.Trim() ?? string.Empty,
                DamageRemarks = payload.DamageRemarks?.Trim() ?? string.Empty,
                Remarks = payload.Remarks?.Trim() ?? string.Empty,
                Notes = payload.Notes?.Trim() ?? string.Empty,
                Status = PodStatus.Pending,
                CreatedBy = user,
                CreatedAt = now,
                UpdatedBy = user,
                UpdatedAt = now
            };

            AddAttachments(pod, payload.ProofOfDelivery, "proofOfDelivery", user, now);
            AddAttachments(pod, payload.Signature, "signature", user, now);
            AddAttachments(pod, payload.Photos, "photos", user, now);

            pod.Timeline.Add(new PodTimelineEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Date = now,
                User = user,
                Action = "POD created",
                ToStatus = PodStatus.Pending.ToString(),
                Remarks = "Delivery confirmation initiated"
            });

            _db.DeliveryConfirmations.Add(pod);
            await _db.SaveChangesAsync(cancellationToken);

            return MapToDto(pod);
        }

        public async Task<PodDto> UpdatePodAsync(int id, PodUpdateRequestDto payload, string currentUser, CancellationToken cancellationToken = default)
        {
            var pod = await _db.DeliveryConfirmations
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (pod == null)
                throw new KeyNotFoundException($"POD with ID {id} not found.");

            if (pod.Status != PodStatus.Pending)
                throw new InvalidOperationException("Only pending PODs can be edited.");

            if (string.IsNullOrWhiteSpace(payload.ReceiverName) || string.IsNullOrWhiteSpace(payload.ReceiverContact))
                throw new ArgumentException("Receiver name and contact are required.");

            var user = string.IsNullOrWhiteSpace(currentUser) ? "System" : currentUser;
            var now = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(payload.DeliveryDate) &&
                DateTime.TryParse(payload.DeliveryDate, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dd))
            {
                pod.DeliveryDate = DateTime.SpecifyKind(dd.Date, DateTimeKind.Utc);
            }

            pod.DispatchId = payload.DispatchId;
            pod.DispatchNumber = payload.DispatchNumber?.Trim() ?? pod.DispatchNumber;
            pod.CustomerId = payload.CustomerId;
            pod.CustomerName = payload.CustomerName?.Trim() ?? pod.CustomerName;
            pod.ReceiverName = payload.ReceiverName.Trim();
            pod.ReceiverContact = payload.ReceiverContact.Trim();
            pod.DeliveryRemarks = payload.DeliveryRemarks?.Trim() ?? string.Empty;
            pod.DamageRemarks = payload.DamageRemarks?.Trim() ?? string.Empty;
            pod.Remarks = payload.Remarks?.Trim() ?? string.Empty;
            pod.Notes = payload.Notes?.Trim() ?? string.Empty;
            pod.UpdatedBy = user;
            pod.UpdatedAt = now;

            if (payload.ProofOfDelivery != null)
            {
                var existingProof = pod.Attachments.Where(x => x.Kind == "proofOfDelivery").ToList();
                foreach (var ep in existingProof) _db.PodAttachments.Remove(ep);
                AddAttachments(pod, payload.ProofOfDelivery, "proofOfDelivery", user, now);
            }

            if (payload.Signature != null)
            {
                var existingSig = pod.Attachments.Where(x => x.Kind == "signature").ToList();
                foreach (var es in existingSig) _db.PodAttachments.Remove(es);
                AddAttachments(pod, payload.Signature, "signature", user, now);
            }

            if (payload.Photos != null)
            {
                var existingPhotos = pod.Attachments.Where(x => x.Kind == "photos").ToList();
                foreach (var ep in existingPhotos) _db.PodAttachments.Remove(ep);
                AddAttachments(pod, payload.Photos, "photos", user, now);
            }

            pod.Timeline.Add(new PodTimelineEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Date = now,
                User = user,
                Action = "POD updated",
                FromStatus = pod.Status.ToString(),
                ToStatus = pod.Status.ToString()
            });

            await _db.SaveChangesAsync(cancellationToken);
            return MapToDto(pod);
        }

        public async Task DeletePodAsync(int id, CancellationToken cancellationToken = default)
        {
            var pod = await _db.DeliveryConfirmations
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (pod == null)
                throw new KeyNotFoundException($"POD with ID {id} not found.");

            if (pod.Status != PodStatus.Pending)
                throw new InvalidOperationException("Only pending PODs can be deleted.");

            _db.DeliveryConfirmations.Remove(pod);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<PodDto> MarkPodDeliveredAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default)
        {
            return await TransitionPodAsync(id, new[] { PodStatus.Pending }, PodStatus.Delivered, "Marked delivered", payload?.Remarks, currentUser, cancellationToken);
        }

        public async Task<PodDto> ConfirmDeliveryAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default)
        {
            return await TransitionPodAsync(id, new[] { PodStatus.Delivered }, PodStatus.Confirmed, "Delivery confirmed", payload?.Remarks, currentUser, cancellationToken);
        }

        public async Task<PodDto> ClosePodAsync(int id, StatusActionRequestDto? payload, string currentUser, CancellationToken cancellationToken = default)
        {
            return await TransitionPodAsync(id, new[] { PodStatus.Confirmed }, PodStatus.Closed, "POD closed", payload?.Remarks, currentUser, cancellationToken);
        }

        public async Task<PodDashboardDto> GetPodDashboardAsync(CancellationToken cancellationToken = default)
        {
            var pending = await _db.DeliveryConfirmations
                .CountAsync(x => x.Status == PodStatus.Pending, cancellationToken);

            var delivered = await _db.DeliveryConfirmations
                .CountAsync(x => x.Status == PodStatus.Delivered, cancellationToken);

            var confirmed = await _db.DeliveryConfirmations
                .CountAsync(x => x.Status == PodStatus.Confirmed || x.Status == PodStatus.Closed, cancellationToken);

            return new PodDashboardDto
            {
                PendingPod = pending,
                Delivered = delivered,
                Confirmed = confirmed
            };
        }

        private async Task<PodDto> TransitionPodAsync(int id, PodStatus[] allowedStatuses, PodStatus nextStatus, string actionName, string? remarks, string currentUser, CancellationToken cancellationToken)
        {
            var pod = await _db.DeliveryConfirmations
                .Include(x => x.Attachments)
                .Include(x => x.Timeline)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (pod == null)
                throw new KeyNotFoundException($"POD with ID {id} not found.");

            if (!allowedStatuses.Contains(pod.Status))
                throw new InvalidOperationException($"Cannot transition POD from {pod.Status} to {nextStatus}.");

            var user = string.IsNullOrWhiteSpace(currentUser) ? "System" : currentUser;
            var now = DateTime.UtcNow;
            var from = pod.Status;

            pod.Status = nextStatus;
            pod.UpdatedBy = user;
            pod.UpdatedAt = now;

            pod.Timeline.Add(new PodTimelineEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Date = now,
                User = user,
                Action = actionName,
                FromStatus = from.ToString(),
                ToStatus = nextStatus.ToString(),
                Remarks = remarks?.Trim()
            });

            await _db.SaveChangesAsync(cancellationToken);
            return MapToDto(pod);
        }

        private static void AddAttachments(DeliveryConfirmation pod, IEnumerable<DispatchAttachmentDto>? attachments, string kind, string user, DateTime now)
        {
            if (attachments == null) return;
            foreach (var att in attachments)
            {
                pod.Attachments.Add(new PodAttachment
                {
                    AttachmentId = string.IsNullOrWhiteSpace(att.Id) ? Guid.NewGuid().ToString() : att.Id,
                    Name = att.Name ?? string.Empty,
                    SizeKb = att.SizeKb,
                    UploadedBy = string.IsNullOrWhiteSpace(att.UploadedBy) ? user : att.UploadedBy,
                    UploadedAt = now,
                    Kind = kind
                });
            }
        }

        private static PodDto MapToDto(DeliveryConfirmation p)
        {
            var proof = p.Attachments.Where(x => x.Kind == "proofOfDelivery").Select(ToAttachmentDto).ToList();
            var signature = p.Attachments.Where(x => x.Kind == "signature").Select(ToAttachmentDto).ToList();
            var photos = p.Attachments.Where(x => x.Kind == "photos").Select(ToAttachmentDto).ToList();
            var generic = p.Attachments.Where(x => x.Kind == "document" || string.IsNullOrWhiteSpace(x.Kind)).Select(ToAttachmentDto).ToList();

            return new PodDto
            {
                Id = p.Id,
                PodNumber = p.PodNumber,
                DispatchId = p.DispatchId,
                DispatchNumber = p.DispatchNumber,
                CustomerId = p.CustomerId,
                CustomerName = p.CustomerName,
                DeliveryDate = p.DeliveryDate.ToString("yyyy-MM-dd"),
                ReceiverName = p.ReceiverName,
                ReceiverContact = p.ReceiverContact,
                DeliveryRemarks = p.DeliveryRemarks,
                DamageRemarks = p.DamageRemarks,
                Remarks = p.Remarks,
                Notes = p.Notes,
                Status = p.Status,
                ProofOfDelivery = proof,
                Signature = signature,
                Photos = photos,
                Attachments = generic,
                CreatedBy = p.CreatedBy,
                CreatedAt = p.CreatedAt.ToString("o"),
                UpdatedBy = p.UpdatedBy,
                UpdatedAt = p.UpdatedAt.ToString("o"),
                Timeline = p.Timeline.OrderBy(x => x.Date).Select(x => new DispatchTimelineEventDto
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

        private static DispatchAttachmentDto ToAttachmentDto(PodAttachment a)
        {
            return new DispatchAttachmentDto
            {
                Id = a.AttachmentId,
                Name = a.Name,
                SizeKb = a.SizeKb,
                UploadedBy = a.UploadedBy,
                UploadedAt = a.UploadedAt.ToString("o")
            };
        }
    }
}
