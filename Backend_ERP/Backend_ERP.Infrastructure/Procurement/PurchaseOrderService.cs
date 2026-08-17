using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using ERP.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Procurement
{
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly ERPDbContext _db;
        private readonly PurchaseOrderNumberingService _numberingService;

        public PurchaseOrderService(ERPDbContext db, PurchaseOrderNumberingService numberingService)
        {
            _db = db;
            _numberingService = numberingService;
        }

        public async Task<PagedResult<PurchaseOrderListItemDto>> GetAllAsync(
            PurchaseOrderListQueryDto query,
            CancellationToken cancellationToken = default)
        {
            var q = FilterQuery(_db.PurchaseOrders.AsNoTracking().Where(po => !po.IsDeleted), query);

            var totalCount = await q.CountAsync(cancellationToken);
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 1000);

            var items = await q
                .OrderByDescending(po => po.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = items.Select(MapToListItemDto).ToList();
            return PagedResult<PurchaseOrderListItemDto>.Create(dtos, totalCount, page, pageSize);
        }

        public async Task<PurchaseOrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var po = await _db.PurchaseOrders
                .Include(p => p.Lines)
                .Include(p => p.History.OrderByDescending(h => h.Date))
                .Include(p => p.ApprovalHistory.OrderByDescending(h => h.DecisionDate))
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

            return po is null ? null : MapToDto(po);
        }

        public async Task<PurchaseOrderDto> CreateAsync(
            PurchaseOrderCreateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var poNumber = string.IsNullOrWhiteSpace(request.PurchaseOrderNumber)
                ? await _numberingService.NextPurchaseOrderNumberAsync(cancellationToken)
                : request.PurchaseOrderNumber.Trim();

            var po = new PurchaseOrder
            {
                PurchaseOrderNumber = poNumber,
                ReferenceNumber = request.ReferenceNumber?.Trim() ?? string.Empty,
                VendorName = request.Vendor?.VendorName?.Trim() ?? string.Empty,
                VendorContact = request.Vendor?.VendorContact?.Trim() ?? string.Empty,
                BillingAddress = request.Vendor?.BillingAddress?.Trim() ?? string.Empty,
                ShippingAddress = request.Vendor?.ShippingAddress?.Trim() ?? string.Empty,
                VendorEmail = request.Vendor?.VendorEmail?.Trim() ?? string.Empty,
                VendorPhone = request.Vendor?.VendorPhone?.Trim() ?? string.Empty,
                GstNumber = request.Vendor?.GstNumber?.Trim() ?? string.Empty,
                OrderDate = request.OrderDate.ToUniversalTime(),
                ExpectedDeliveryDate = request.ExpectedDeliveryDate?.ToUniversalTime(),
                PaymentTerms = string.IsNullOrWhiteSpace(request.PaymentTerms) ? "Net 30" : request.PaymentTerms.Trim(),
                DeliveryTerms = string.IsNullOrWhiteSpace(request.DeliveryTerms) ? "Door Delivery" : request.DeliveryTerms.Trim(),
                BuyerName = request.BuyerName?.Trim() ?? string.Empty,
                Notes = request.Notes?.Trim() ?? string.Empty,
                Status = request.Status,
                Remarks = request.Remarks?.Trim() ?? string.Empty,
                Priority = request.Priority,
                Currency = string.IsNullOrWhiteSpace(request.Currency) ? "INR" : request.Currency.Trim(),
                SourceType = request.SourceType,
                SalesOrderNumber = request.SalesOrderNumber?.Trim() ?? string.Empty,
                CreatedAt = now,
                CreatedBy = actingUser,
                UpdatedAt = now,
                UpdatedBy = actingUser
            };

            if (!string.IsNullOrWhiteSpace(request.Vendor?.VendorName))
            {
                var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Name.ToLower() == request.Vendor.VendorName.Trim().ToLower() && !v.IsDeleted, cancellationToken);
                if (vendor != null)
                {
                    po.VendorId = vendor.Id;
                }
            }

            foreach (var l in request.Lines ?? new List<PurchaseOrderLineDto>())
            {
                po.Lines.Add(new PurchaseOrderLine
                {
                    ItemName = l.ItemName.Trim(),
                    Description = l.Description?.Trim() ?? string.Empty,
                    Quantity = l.Quantity,
                    Unit = string.IsNullOrWhiteSpace(l.Unit) ? "Nos" : l.Unit.Trim(),
                    Rate = l.Rate,
                    Discount = l.Discount,
                    Tax = l.Tax
                });
            }

            PurchaseOrderRules.CalculateTotals(po.Lines, out var sub, out var disc, out var tax, out var total);
            po.Subtotal = sub;
            po.DiscountTotal = disc;
            po.TaxTotal = tax;
            po.TotalAmount = total;

            if (po.Status == PurchaseOrderStatus.Submitted)
            {
                po.SubmittedDate = now;
                po.ApprovalHistory.Add(new PurchaseOrderApprovalHistory
                {
                    EventKind = "submitted",
                    Decision = "Submitted",
                    Approver = actingUser,
                    Role = "Buyer",
                    DecisionDate = now,
                    Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? "Submitted for approval." : request.Remarks.Trim()
                });
            }

            po.History.Add(new PurchaseOrderStatusHistory
            {
                Status = po.Status,
                PreviousStatus = null,
                Date = now,
                User = actingUser,
                Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? "Purchase order created." : request.Remarks.Trim(),
                Label = "Created"
            });

            _db.PurchaseOrders.Add(po);
            await _db.SaveChangesAsync(cancellationToken);

            return (await GetByIdAsync(po.Id, cancellationToken))!;
        }

        public async Task<PurchaseOrderDto?> UpdateAsync(
            int id,
            PurchaseOrderUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var po = await _db.PurchaseOrders
                .Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

            if (po is null) return null;

            var now = DateTime.UtcNow;

            po.ReferenceNumber = request.ReferenceNumber?.Trim() ?? po.ReferenceNumber;
            if (request.Vendor != null)
            {
                po.VendorName = request.Vendor.VendorName?.Trim() ?? po.VendorName;
                po.VendorContact = request.Vendor.VendorContact?.Trim() ?? po.VendorContact;
                po.BillingAddress = request.Vendor.BillingAddress?.Trim() ?? po.BillingAddress;
                po.ShippingAddress = request.Vendor.ShippingAddress?.Trim() ?? po.ShippingAddress;
                po.VendorEmail = request.Vendor.VendorEmail?.Trim() ?? po.VendorEmail;
                po.VendorPhone = request.Vendor.VendorPhone?.Trim() ?? po.VendorPhone;
                po.GstNumber = request.Vendor.GstNumber?.Trim() ?? po.GstNumber;
            }

            po.BuyerName = request.BuyerName?.Trim() ?? po.BuyerName;
            po.Notes = request.Notes?.Trim() ?? po.Notes;
            po.OrderDate = request.OrderDate.ToUniversalTime();
            po.ExpectedDeliveryDate = request.ExpectedDeliveryDate?.ToUniversalTime();
            po.PaymentTerms = request.PaymentTerms?.Trim() ?? po.PaymentTerms;
            po.DeliveryTerms = request.DeliveryTerms?.Trim() ?? po.DeliveryTerms;
            po.SourceType = request.SourceType;
            po.SalesOrderNumber = request.SalesOrderNumber?.Trim() ?? po.SalesOrderNumber;
            po.Priority = request.Priority;
            po.Currency = string.IsNullOrWhiteSpace(request.Currency) ? po.Currency : request.Currency.Trim();
            po.Remarks = request.Remarks?.Trim() ?? po.Remarks;
            po.UpdatedAt = now;
            po.UpdatedBy = actingUser;

            _db.PurchaseOrderLines.RemoveRange(po.Lines);
            po.Lines.Clear();

            foreach (var l in request.Lines ?? new List<PurchaseOrderLineDto>())
            {
                po.Lines.Add(new PurchaseOrderLine
                {
                    PurchaseOrderId = id,
                    ItemName = l.ItemName.Trim(),
                    Description = l.Description?.Trim() ?? string.Empty,
                    Quantity = l.Quantity,
                    Unit = string.IsNullOrWhiteSpace(l.Unit) ? "Nos" : l.Unit.Trim(),
                    Rate = l.Rate,
                    Discount = l.Discount,
                    Tax = l.Tax
                });
            }

            PurchaseOrderRules.CalculateTotals(po.Lines, out var sub, out var disc, out var tax, out var total);
            po.Subtotal = sub;
            po.DiscountTotal = disc;
            po.TaxTotal = tax;
            po.TotalAmount = total;

            await _db.SaveChangesAsync(cancellationToken);
            return (await GetByIdAsync(id, cancellationToken))!;
        }

        public async Task<bool> DeleteAsync(int id, string actingUser, CancellationToken cancellationToken = default)
        {
            var po = await _db.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
            if (po is null) return false;

            po.IsDeleted = true;
            po.UpdatedAt = DateTime.UtcNow;
            po.UpdatedBy = actingUser;

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<PurchaseOrderDto?> UpdateStatusAsync(
            int id,
            PurchaseOrderStatusUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            return await ChangeStatusAsync(id, request.Status, request.Remarks ?? string.Empty, actingUser, cancellationToken);
        }

        public async Task<PurchaseOrderDto?> CancelAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default)
        {
            return await ChangeStatusAsync(id, PurchaseOrderStatus.Cancelled, remarks, actingUser, cancellationToken);
        }

        public async Task<List<PurchaseOrderHistoryDto>> GetStatusHistoryAsync(int id, CancellationToken cancellationToken = default)
        {
            var histories = await _db.PurchaseOrderStatusHistories
                .AsNoTracking()
                .Where(h => h.PurchaseOrderId == id)
                .OrderByDescending(h => h.Date)
                .ToListAsync(cancellationToken);

            return histories.Select(MapToHistoryDto).ToList();
        }

        public async Task<List<PurchaseOrderApprovalQueueItemDto>> GetApprovalQueueAsync(
            PurchaseOrderListQueryDto query,
            CancellationToken cancellationToken = default)
        {
            var q = FilterQuery(_db.PurchaseOrders.AsNoTracking().Where(po => !po.IsDeleted), query);

            q = q.Where(po => po.Status == PurchaseOrderStatus.Submitted || po.Status == PurchaseOrderStatus.RevisionRequired);

            var items = await q.OrderByDescending(po => po.SubmittedDate ?? po.CreatedAt).ToListAsync(cancellationToken);

            return items.Select(po => new PurchaseOrderApprovalQueueItemDto
            {
                Id = po.Id,
                PurchaseOrderNumber = po.PurchaseOrderNumber,
                VendorName = po.VendorName,
                SubmittedBy = po.CreatedBy,
                SubmittedDate = po.SubmittedDate ?? po.CreatedAt,
                Amount = po.TotalAmount,
                Priority = po.Priority,
                Status = po.Status,
                ApprovalStatus = po.Status.ToString()
            }).ToList();
        }

        public async Task<PurchaseOrderApprovalMetricsDto> GetApprovalMetricsAsync(
            PurchaseOrderListQueryDto query,
            CancellationToken cancellationToken = default)
        {
            var q = _db.PurchaseOrders.AsNoTracking().Where(po => !po.IsDeleted);
            var today = DateTime.UtcNow.Date;

            var pending = await q.CountAsync(po => po.Status == PurchaseOrderStatus.Submitted, cancellationToken);
            var approvedToday = await q.CountAsync(po => po.Status == PurchaseOrderStatus.Approved && po.UpdatedAt >= today, cancellationToken);
            var rejected = await q.CountAsync(po => po.Status == PurchaseOrderStatus.Rejected, cancellationToken);
            var revision = await q.CountAsync(po => po.Status == PurchaseOrderStatus.RevisionRequired, cancellationToken);

            return new PurchaseOrderApprovalMetricsDto
            {
                PendingApproval = pending,
                ApprovedToday = approvedToday,
                Rejected = rejected,
                RevisionRequired = revision,
                AverageApprovalTimeLabel = "1.5 hours"
            };
        }

        public async Task<PurchaseOrderDto?> ApproveAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default)
        {
            return await MutateApprovalAsync(id, PurchaseOrderStatus.Approved, "approved", "Approved", remarks, actingUser, cancellationToken);
        }

        public async Task<PurchaseOrderDto?> RejectAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default)
        {
            return await MutateApprovalAsync(id, PurchaseOrderStatus.Rejected, "rejected", "Rejected", remarks, actingUser, cancellationToken);
        }

        public async Task<PurchaseOrderDto?> RequestRevisionAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default)
        {
            return await MutateApprovalAsync(id, PurchaseOrderStatus.RevisionRequired, "revision_requested", "Revision Requested", remarks, actingUser, cancellationToken);
        }

        public async Task<PurchaseOrderDto?> ReopenAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default)
        {
            return await MutateApprovalAsync(id, PurchaseOrderStatus.Draft, "reopened", "Reopened", remarks, actingUser, cancellationToken);
        }

        public async Task<List<PurchaseOrderApprovalHistoryDto>> GetApprovalHistoryAsync(int id, CancellationToken cancellationToken = default)
        {
            var list = await _db.PurchaseOrderApprovalHistories
                .AsNoTracking()
                .Where(h => h.PurchaseOrderId == id)
                .OrderByDescending(h => h.DecisionDate)
                .ToListAsync(cancellationToken);

            return list.Select(MapToApprovalHistoryDto).ToList();
        }

        public async Task<string> GetNextNumberAsync(CancellationToken cancellationToken = default)
        {
            return await _numberingService.NextPurchaseOrderNumberAsync(cancellationToken);
        }

        private async Task<PurchaseOrderDto?> MutateApprovalAsync(
            int id,
            PurchaseOrderStatus targetStatus,
            string eventKind,
            string decision,
            string remarks,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var po = await _db.PurchaseOrders
                .Include(p => p.History)
                .Include(p => p.ApprovalHistory)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

            if (po is null) return null;

            if (!PurchaseOrderRules.CanTransition(po.Status, targetStatus))
            {
                throw new InvalidOperationException($"Cannot transition purchase order status from {po.Status} to {targetStatus}.");
            }

            var prev = po.Status;
            var now = DateTime.UtcNow;

            po.Status = targetStatus;
            po.UpdatedAt = now;
            po.UpdatedBy = actingUser;

            var rmk = string.IsNullOrWhiteSpace(remarks) ? $"Decision: {decision}" : remarks.Trim();

            po.ApprovalHistory.Add(new PurchaseOrderApprovalHistory
            {
                PurchaseOrderId = id,
                EventKind = eventKind,
                Decision = decision,
                Approver = actingUser,
                Role = "Procurement Manager",
                DecisionDate = now,
                Remarks = rmk
            });

            po.History.Add(new PurchaseOrderStatusHistory
            {
                PurchaseOrderId = id,
                Status = targetStatus,
                PreviousStatus = prev,
                Date = now,
                User = actingUser,
                Remarks = rmk,
                Label = decision
            });

            await _db.SaveChangesAsync(cancellationToken);
            return (await GetByIdAsync(id, cancellationToken))!;
        }

        private async Task<PurchaseOrderDto?> ChangeStatusAsync(
            int id,
            PurchaseOrderStatus targetStatus,
            string remarks,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var po = await _db.PurchaseOrders
                .Include(p => p.History)
                .Include(p => p.ApprovalHistory)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

            if (po is null) return null;

            if (!PurchaseOrderRules.CanTransition(po.Status, targetStatus))
            {
                throw new InvalidOperationException($"Cannot transition purchase order status from {po.Status} to {targetStatus}.");
            }

            var prev = po.Status;
            var now = DateTime.UtcNow;

            po.Status = targetStatus;
            po.UpdatedAt = now;
            po.UpdatedBy = actingUser;

            if (targetStatus == PurchaseOrderStatus.Submitted && !po.SubmittedDate.HasValue)
            {
                po.SubmittedDate = now;
            }

            var rmk = string.IsNullOrWhiteSpace(remarks) ? $"Status changed to {targetStatus}." : remarks.Trim();

            po.History.Add(new PurchaseOrderStatusHistory
            {
                PurchaseOrderId = id,
                Status = targetStatus,
                PreviousStatus = prev,
                Date = now,
                User = actingUser,
                Remarks = rmk,
                Label = targetStatus.ToString()
            });

            await _db.SaveChangesAsync(cancellationToken);
            return (await GetByIdAsync(id, cancellationToken))!;
        }

        private static IQueryable<PurchaseOrder> FilterQuery(IQueryable<PurchaseOrder> q, PurchaseOrderListQueryDto query)
        {
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                q = q.Where(po =>
                    po.PurchaseOrderNumber.ToLower().Contains(term) ||
                    po.VendorName.ToLower().Contains(term) ||
                    po.ReferenceNumber.ToLower().Contains(term) ||
                    po.BuyerName.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<PurchaseOrderStatus>(query.Status, true, out var statusEnum))
            {
                q = q.Where(po => po.Status == statusEnum);
            }

            if (!string.IsNullOrWhiteSpace(query.VendorName))
            {
                var vTerm = query.VendorName.Trim().ToLower();
                q = q.Where(po => po.VendorName.ToLower().Contains(vTerm));
            }

            if (!string.IsNullOrWhiteSpace(query.Priority) && Enum.TryParse<PurchaseOrderPriority>(query.Priority, true, out var prioEnum))
            {
                q = q.Where(po => po.Priority == prioEnum);
            }

            if (query.DateFrom.HasValue)
            {
                q = q.Where(po => po.OrderDate >= query.DateFrom.Value.ToUniversalTime());
            }

            if (query.DateTo.HasValue)
            {
                q = q.Where(po => po.OrderDate <= query.DateTo.Value.ToUniversalTime());
            }

            return q;
        }

        private static PurchaseOrderListItemDto MapToListItemDto(PurchaseOrder po) => new()
        {
            Id = po.Id,
            PurchaseOrderNumber = po.PurchaseOrderNumber,
            VendorName = po.VendorName,
            ReferenceNumber = po.ReferenceNumber,
            OrderDate = po.OrderDate,
            ExpectedDeliveryDate = po.ExpectedDeliveryDate,
            Amount = po.TotalAmount,
            Status = po.Status,
            CreatedBy = po.CreatedBy,
            UpdatedAt = po.UpdatedAt,
            CreatedAt = po.CreatedAt,
            Remarks = po.Remarks
        };

        private static PurchaseOrderDto MapToDto(PurchaseOrder po) => new()
        {
            Id = po.Id,
            PurchaseOrderNumber = po.PurchaseOrderNumber,
            ReferenceNumber = po.ReferenceNumber,
            Vendor = new PurchaseOrderVendorDto
            {
                VendorName = po.VendorName,
                VendorContact = po.VendorContact,
                BillingAddress = po.BillingAddress,
                ShippingAddress = po.ShippingAddress,
                VendorEmail = po.VendorEmail,
                VendorPhone = po.VendorPhone,
                GstNumber = po.GstNumber
            },
            BuyerName = po.BuyerName,
            Notes = po.Notes,
            OrderDate = po.OrderDate,
            ExpectedDeliveryDate = po.ExpectedDeliveryDate,
            PaymentTerms = po.PaymentTerms,
            DeliveryTerms = po.DeliveryTerms,
            SourceType = po.SourceType,
            SalesOrderNumber = po.SalesOrderNumber,
            Priority = po.Priority,
            Currency = po.Currency,
            Subtotal = po.Subtotal,
            DiscountTotal = po.DiscountTotal,
            TaxTotal = po.TaxTotal,
            TotalAmount = po.TotalAmount,
            Status = po.Status,
            Remarks = po.Remarks,
            SubmittedDate = po.SubmittedDate,
            CreatedBy = po.CreatedBy,
            CreatedDate = po.CreatedAt,
            UpdatedBy = po.UpdatedBy,
            UpdatedDate = po.UpdatedAt,
            Lines = po.Lines?.Select(l => new PurchaseOrderLineDto
            {
                Id = l.Id.ToString(),
                ItemName = l.ItemName,
                Description = l.Description,
                Quantity = l.Quantity,
                Unit = l.Unit,
                Rate = l.Rate,
                Discount = l.Discount,
                Tax = l.Tax,
                Amount = l.Amount
            }).ToList() ?? new(),
            History = po.History?.Select(MapToHistoryDto).ToList() ?? new(),
            ApprovalHistory = po.ApprovalHistory?.Select(MapToApprovalHistoryDto).ToList() ?? new()
        };

        private static PurchaseOrderHistoryDto MapToHistoryDto(PurchaseOrderStatusHistory h) => new()
        {
            Id = h.Id.ToString(),
            Status = h.Status,
            PreviousStatus = h.PreviousStatus,
            Date = h.Date,
            User = h.User,
            Remarks = h.Remarks,
            Label = string.IsNullOrWhiteSpace(h.Label) ? h.Status.ToString() : h.Label
        };

        private static PurchaseOrderApprovalHistoryDto MapToApprovalHistoryDto(PurchaseOrderApprovalHistory h) => new()
        {
            Id = h.Id.ToString(),
            EventKind = h.EventKind,
            Decision = h.Decision,
            Approver = h.Approver,
            Role = h.Role,
            DecisionDate = h.DecisionDate,
            Remarks = h.Remarks
        };
    }
}
