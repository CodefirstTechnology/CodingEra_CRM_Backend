using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using ERP.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Infrastructure.Procurement
{
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly ERPDbContext _db;
        private readonly PurchaseOrderNumberingService _numberingService;
        private readonly IPurchaseOrderApprovalEngine _approvalEngine;
        private readonly ILogger<PurchaseOrderService> _logger;

        public PurchaseOrderService(
            ERPDbContext db, 
            PurchaseOrderNumberingService numberingService,
            IPurchaseOrderApprovalEngine approvalEngine,
            ILogger<PurchaseOrderService> logger)
        {
            _db = db;
            _numberingService = numberingService;
            _approvalEngine = approvalEngine;
            _logger = logger;
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
                .Include(p => p.Approvals.OrderByDescending(a => a.ActionedAt))
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
                RevisionNumber = 0,
                IsCurrentRevision = true,
                ReferenceNumber = request.ReferenceNumber?.Trim() ?? string.Empty,
                VendorName = request.Vendor?.VendorName?.Trim() ?? string.Empty,
                VendorCode = request.Vendor?.VendorCode?.Trim() ?? string.Empty,
                VendorContact = request.Vendor?.VendorContact?.Trim() ?? string.Empty,
                BillingAddress = request.Vendor?.BillingAddress?.Trim() ?? string.Empty,
                ShippingAddress = request.Vendor?.ShippingAddress?.Trim() ?? string.Empty,
                VendorEmail = request.Vendor?.VendorEmail?.Trim() ?? string.Empty,
                VendorPhone = request.Vendor?.VendorPhone?.Trim() ?? string.Empty,
                GstNumber = request.Vendor?.GstNumber?.Trim() ?? string.Empty,
                VendorGstin = request.Vendor?.GstNumber?.Trim() ?? string.Empty,
                VendorStateCode = request.Vendor?.StateCode?.Trim() ?? string.Empty,
                VendorPan = request.Vendor?.Pan?.Trim() ?? string.Empty,
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
                    if (string.IsNullOrWhiteSpace(po.VendorCode)) po.VendorCode = vendor.VendorCode;
                    if (string.IsNullOrWhiteSpace(po.VendorGstin)) po.VendorGstin = vendor.GSTIN;
                    if (string.IsNullOrWhiteSpace(po.VendorStateCode) && vendor.GSTIN?.Length >= 2) po.VendorStateCode = vendor.GSTIN.Substring(0, 2);
                    if (string.IsNullOrWhiteSpace(po.VendorPan)) po.VendorPan = vendor.PAN;
                }
            }

            int lineNum = 1;
            foreach (var l in request.Lines ?? new List<PurchaseOrderLineDto>())
            {
                decimal tol = l.OverDeliveryTolerancePercent;
                if (!IsBulkCommodity(l.Unit)) tol = 0m;
                else if (tol > 2m) tol = 2m;

                po.Lines.Add(new PurchaseOrderLine
                {
                    LineNumber = lineNum++,
                    ItemName = l.ItemName.Trim(),
                    ItemCode = l.ItemCode?.Trim() ?? string.Empty,
                    ItemType = l.ItemType,
                    Description = l.Description?.Trim() ?? string.Empty,
                    HsnCode = l.HsnCode?.Trim() ?? string.Empty,
                    Quantity = l.Quantity,
                    OrderedQuantity = l.Quantity,
                    Unit = string.IsNullOrWhiteSpace(l.Unit) ? "Nos" : l.Unit.Trim(),
                    BaseUom = string.IsNullOrWhiteSpace(l.BaseUom) ? (l.Unit ?? "Nos") : l.BaseUom.Trim(),
                    Rate = l.Rate,
                    UnitRate = l.Rate,
                    Discount = l.Discount,
                    DiscountPercent = l.Discount,
                    Tax = l.Tax,
                    TaxAmount = l.Tax,
                    OverDeliveryTolerancePercent = tol,
                    DeliveryScheduleJson = string.IsNullOrWhiteSpace(l.DeliveryScheduleJson) ? "[]" : l.DeliveryScheduleJson,
                    LineStatus = PurchaseOrderLineStatus.Pending
                });
            }

            PurchaseOrderRules.CalculateTotals(po.Lines, out var sub, out var disc, out var tax, out var total);
            po.Subtotal = sub;
            po.DiscountTotal = disc;
            po.TaxTotal = tax;
            po.TotalAmount = total;
            po.GrandTotalBaseCurrency = Math.Round(total * po.ExchangeRate, 2, MidpointRounding.AwayFromZero);

            if (po.Status == PurchaseOrderStatus.Submitted || po.Status == PurchaseOrderStatus.PendingApproval)
            {
                po.Status = PurchaseOrderStatus.PendingApproval;
                po.SubmittedDate = now;

                var (tier, isEx, reasons) = await _approvalEngine.EvaluateDOARequirementsAsync(po, null, false, false, cancellationToken);
                po.RequiredApprovalTier = tier;
                po.IsExceptionApproval = isEx;
                po.ExceptionReasonsJson = JsonSerializer.Serialize(reasons);

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

        public async Task<PurchaseOrderDto> CreateDirectPOAsync(
            DirectPOCreateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var vendor = await _db.Vendors
                .Include(v => v.Contacts)
                .Include(v => v.Addresses)
                .FirstOrDefaultAsync(v => v.Id == request.VendorId && !v.IsDeleted, cancellationToken);

            if (vendor == null)
            {
                throw new KeyNotFoundException($"Vendor ID {request.VendorId} was not found.");
            }

            if (vendor.Status is VendorStatus.Blocked or VendorStatus.Disqualified or VendorStatus.Inactive or VendorStatus.OnHold)
            {
                throw new InvalidOperationException($"Vendor '{vendor.Name}' is in {vendor.Status} status and cannot receive direct purchase orders.");
            }

            var poNumber = await _numberingService.NextPurchaseOrderNumberAsync(cancellationToken);

            string defaultBilling = vendor.Addresses?.FirstOrDefault(a => a.AddressType == VendorAddressType.Billing)?.AddressLine1 ?? string.Empty;
            string defaultShipping = vendor.Addresses?.FirstOrDefault(a => a.AddressType == VendorAddressType.Shipping)?.AddressLine1 ?? defaultBilling;
            string defaultContact = vendor.Contacts != null && vendor.Contacts.Count > 0 
                ? $"{vendor.Contacts[0].FirstName} {vendor.Contacts[0].LastName}".Trim() 
                : string.Empty;

            var po = new PurchaseOrder
            {
                PurchaseOrderNumber = poNumber,
                RevisionNumber = 0,
                IsCurrentRevision = true,
                VendorId = vendor.Id,
                VendorName = vendor.Name,
                VendorCode = vendor.VendorCode,
                VendorGstin = !string.IsNullOrWhiteSpace(vendor.GSTIN) ? vendor.GSTIN : request.VendorGstin,
                VendorStateCode = vendor.GSTIN?.Length >= 2 ? vendor.GSTIN.Substring(0, 2) : request.VendorStateCode,
                VendorPan = !string.IsNullOrWhiteSpace(vendor.PAN) ? vendor.PAN : request.VendorPan,
                VendorContact = defaultContact,
                VendorEmail = vendor.Email ?? string.Empty,
                VendorPhone = vendor.Phone ?? string.Empty,
                BillingAddress = request.BillingAddress ?? defaultBilling,
                ShippingAddress = request.ShippingAddress ?? defaultShipping,
                BuyerName = request.BuyerName ?? string.Empty,
                OrderDate = request.OrderDate.ToUniversalTime(),
                ExpectedDeliveryDate = request.ExpectedDeliveryDate?.ToUniversalTime(),
                Currency = string.IsNullOrWhiteSpace(request.Currency) ? "INR" : request.Currency.Trim(),
                ExchangeRate = request.ExchangeRate > 0 ? request.ExchangeRate : 1.0m,
                PaymentTermsDays = request.PaymentTermsDays,
                PaymentTerms = request.PaymentTerms,
                Incoterms = request.Incoterms,
                DeliveryTerms = request.DeliveryTerms,
                Priority = request.Priority,
                SourceType = PurchaseOrderSourceType.Manual,
                Notes = request.Notes ?? string.Empty,
                Remarks = request.Remarks ?? string.Empty,
                Status = PurchaseOrderStatus.PendingApproval,
                SubmittedDate = now,
                CreatedAt = now,
                CreatedBy = actingUser,
                UpdatedAt = now,
                UpdatedBy = actingUser
            };

            decimal subtotal = 0m;
            decimal discountTotal = 0m;
            decimal totalCgst = 0m;
            decimal totalSgst = 0m;
            decimal totalIgst = 0m;
            decimal taxTotal = 0m;

            bool isInterState = !string.IsNullOrWhiteSpace(po.VendorStateCode) && 
                                !string.Equals(po.VendorStateCode, "27", StringComparison.OrdinalIgnoreCase);

            int lineNum = 1;
            foreach (var lineDto in request.Lines ?? new List<EnterprisePOLineInputDto>())
            {
                decimal tol = lineDto.OverDeliveryTolerancePercent;
                if (!IsBulkCommodity(lineDto.Unit)) tol = 0m;
                else if (tol > 2m) tol = 2m;

                if (lineDto.DeliverySchedules != null && lineDto.DeliverySchedules.Count > 0)
                {
                    decimal schedSum = lineDto.DeliverySchedules.Sum(s => s.Quantity);
                    if (Math.Abs(schedSum - lineDto.Quantity) > 0.0001m)
                    {
                        throw new InvalidOperationException($"Line {lineNum} ({lineDto.ItemName}): Delivery schedule total ({schedSum}) must match ordered quantity ({lineDto.Quantity}).");
                    }
                }

                decimal gross = Math.Round(lineDto.Quantity * lineDto.Rate, 2, MidpointRounding.AwayFromZero);
                decimal disc = Math.Round(gross * (lineDto.DiscountPercent / 100m), 2, MidpointRounding.AwayFromZero);
                decimal taxable = gross - disc;

                decimal cgst = 0m, sgst = 0m, igst = 0m;
                if (isInterState)
                {
                    igst = Math.Round(taxable * (lineDto.GstRatePercentage / 100m), 2, MidpointRounding.AwayFromZero);
                }
                else
                {
                    decimal half = lineDto.GstRatePercentage / 2m;
                    cgst = Math.Round(taxable * (half / 100m), 2, MidpointRounding.AwayFromZero);
                    sgst = Math.Round(taxable * (half / 100m), 2, MidpointRounding.AwayFromZero);
                }

                decimal lineTax = cgst + sgst + igst;
                decimal lineTot = taxable + lineTax;

                po.Lines.Add(new PurchaseOrderLine
                {
                    LineNumber = lineNum++,
                    ItemType = lineDto.ItemType,
                    ItemCode = lineDto.ItemCode ?? string.Empty,
                    ItemName = lineDto.ItemName,
                    Description = lineDto.Description ?? string.Empty,
                    HsnCode = lineDto.HsnCode ?? string.Empty,
                    Unit = lineDto.Unit ?? "Nos",
                    BaseUom = lineDto.Unit ?? "Nos",
                    Quantity = lineDto.Quantity,
                    OrderedQuantity = lineDto.Quantity,
                    Rate = lineDto.Rate,
                    UnitRate = lineDto.Rate,
                    Discount = lineDto.DiscountPercent,
                    DiscountPercent = lineDto.DiscountPercent,
                    DiscountAmount = disc,
                    TaxableAmount = taxable,
                    GstRatePercentage = lineDto.GstRatePercentage,
                    CgstAmount = cgst,
                    SgstAmount = sgst,
                    IgstAmount = igst,
                    Tax = lineTax,
                    TaxAmount = lineTax,
                    Amount = lineTot,
                    LineTotal = lineTot,
                    OverDeliveryTolerancePercent = tol,
                    ExpectedDeliveryDate = lineDto.ExpectedDeliveryDate ?? po.ExpectedDeliveryDate,
                    TargetWarehouseId = lineDto.TargetWarehouseId,
                    DeliveryScheduleJson = lineDto.DeliverySchedules != null && lineDto.DeliverySchedules.Count > 0
                        ? JsonSerializer.Serialize(lineDto.DeliverySchedules)
                        : "[]",
                    LineStatus = PurchaseOrderLineStatus.Pending
                });

                subtotal += taxable;
                discountTotal += disc;
                totalCgst += cgst;
                totalSgst += sgst;
                totalIgst += igst;
                taxTotal += lineTax;
            }

            po.Subtotal = subtotal;
            po.DiscountTotal = discountTotal;
            po.TotalCgstAmount = totalCgst;
            po.TotalSgstAmount = totalSgst;
            po.TotalIgstAmount = totalIgst;
            po.TaxTotal = taxTotal;
            po.TotalAmount = subtotal + taxTotal;
            po.GrandTotalBaseCurrency = Math.Round(po.TotalAmount * po.ExchangeRate, 2, MidpointRounding.AwayFromZero);

            // DOA Tier Evaluation with Exception Escalations
            var (tier, isEx, reasons) = await _approvalEngine.EvaluateDOARequirementsAsync(
                po, 
                vendor.RatingScore, 
                request.IsUnbudgetedCapEx, 
                request.IsNonAvlEmergencyOrder, 
                cancellationToken);

            po.RequiredApprovalTier = tier;
            po.IsExceptionApproval = isEx;
            po.ExceptionReasonsJson = JsonSerializer.Serialize(reasons);

            po.History.Add(new PurchaseOrderStatusHistory
            {
                Status = PurchaseOrderStatus.PendingApproval,
                PreviousStatus = null,
                Date = now,
                User = actingUser,
                Remarks = "Direct purchase order generated and submitted to Delegation of Authority queue.",
                Label = "Submitted"
            });

            _db.PurchaseOrders.Add(po);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Direct PO {PONumber} created for Vendor {VendorName} with Tier {Tier}", po.PurchaseOrderNumber, po.VendorName, tier);
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
                po.VendorCode = request.Vendor.VendorCode?.Trim() ?? po.VendorCode;
                po.VendorContact = request.Vendor.VendorContact?.Trim() ?? po.VendorContact;
                po.BillingAddress = request.Vendor.BillingAddress?.Trim() ?? po.BillingAddress;
                po.ShippingAddress = request.Vendor.ShippingAddress?.Trim() ?? po.ShippingAddress;
                po.VendorEmail = request.Vendor.VendorEmail?.Trim() ?? po.VendorEmail;
                po.VendorPhone = request.Vendor.VendorPhone?.Trim() ?? po.VendorPhone;
                po.GstNumber = request.Vendor.GstNumber?.Trim() ?? po.GstNumber;
                po.VendorGstin = request.Vendor.GstNumber?.Trim() ?? po.VendorGstin;
                po.VendorStateCode = request.Vendor.StateCode?.Trim() ?? po.VendorStateCode;
                po.VendorPan = request.Vendor.Pan?.Trim() ?? po.VendorPan;
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

            int lineNum = 1;
            foreach (var l in request.Lines ?? new List<PurchaseOrderLineDto>())
            {
                decimal tol = l.OverDeliveryTolerancePercent;
                if (!IsBulkCommodity(l.Unit)) tol = 0m;
                else if (tol > 2m) tol = 2m;

                po.Lines.Add(new PurchaseOrderLine
                {
                    LineNumber = lineNum++,
                    ItemName = l.ItemName.Trim(),
                    ItemCode = l.ItemCode?.Trim() ?? string.Empty,
                    ItemType = l.ItemType,
                    Description = l.Description?.Trim() ?? string.Empty,
                    HsnCode = l.HsnCode?.Trim() ?? string.Empty,
                    Quantity = l.Quantity,
                    OrderedQuantity = l.Quantity,
                    Unit = string.IsNullOrWhiteSpace(l.Unit) ? "Nos" : l.Unit.Trim(),
                    BaseUom = string.IsNullOrWhiteSpace(l.BaseUom) ? (l.Unit ?? "Nos") : l.BaseUom.Trim(),
                    Rate = l.Rate,
                    UnitRate = l.Rate,
                    Discount = l.Discount,
                    DiscountPercent = l.Discount,
                    Tax = l.Tax,
                    TaxAmount = l.Tax,
                    OverDeliveryTolerancePercent = tol,
                    DeliveryScheduleJson = string.IsNullOrWhiteSpace(l.DeliveryScheduleJson) ? "[]" : l.DeliveryScheduleJson,
                    LineStatus = PurchaseOrderLineStatus.Pending
                });
            }

            PurchaseOrderRules.CalculateTotals(po.Lines, out var sub, out var disc, out var tax, out var total);
            po.Subtotal = sub;
            po.DiscountTotal = disc;
            po.TaxTotal = tax;
            po.TotalAmount = total;
            po.GrandTotalBaseCurrency = Math.Round(total * po.ExchangeRate, 2, MidpointRounding.AwayFromZero);

            await _db.SaveChangesAsync(cancellationToken);
            return (await GetByIdAsync(po.Id, cancellationToken))!;
        }

        public async Task<PurchaseOrderDto?> SubmitForApprovalAsync(
            int id, 
            string actingUser, 
            CancellationToken cancellationToken = default)
        {
            var po = await _db.PurchaseOrders
                .Include(p => p.Lines)
                .Include(p => p.Vendor)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

            if (po is null) return null;

            if (po.Status != PurchaseOrderStatus.Draft && po.Status != PurchaseOrderStatus.Rejected)
            {
                throw new InvalidOperationException($"Cannot submit purchase order in '{po.Status}' status for approval.");
            }

            var now = DateTime.UtcNow;
            po.Status = PurchaseOrderStatus.PendingApproval;
            po.SubmittedDate = now;
            po.CurrentApprovalTier = PurchaseOrderApprovalTier.None;

            decimal? vendorRating = po.Vendor?.RatingScore;
            var (tier, isEx, reasons) = await _approvalEngine.EvaluateDOARequirementsAsync(po, vendorRating, false, false, cancellationToken);
            po.RequiredApprovalTier = tier;
            po.IsExceptionApproval = isEx;
            po.ExceptionReasonsJson = JsonSerializer.Serialize(reasons);

            po.History.Add(new PurchaseOrderStatusHistory
            {
                Status = PurchaseOrderStatus.PendingApproval,
                PreviousStatus = PurchaseOrderStatus.Draft,
                Date = now,
                User = actingUser,
                Remarks = "Submitted for Delegation of Authority review.",
                Label = "Submitted"
            });

            po.UpdatedAt = now;
            po.UpdatedBy = actingUser;

            await _db.SaveChangesAsync(cancellationToken);
            return (await GetByIdAsync(id, cancellationToken))!;
        }

        public async Task<bool> DeleteAsync(int id, string actingUser, CancellationToken cancellationToken = default)
        {
            var po = await _db.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
            if (po is null) return false;

            if (po.Status != PurchaseOrderStatus.Draft)
            {
                throw new InvalidOperationException($"Cannot delete purchase order {po.PurchaseOrderNumber} in status {po.Status}. Only Draft orders may be deleted.");
            }

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
                .Where(h => h.PurchaseOrderId == id)
                .OrderByDescending(h => h.Date)
                .ToListAsync(cancellationToken);

            return histories.Select(MapToHistoryDto).ToList();
        }

        public async Task<List<PurchaseOrderApprovalQueueItemDto>> GetApprovalQueueAsync(
            PurchaseOrderListQueryDto query,
            CancellationToken cancellationToken = default)
        {
            var q = _db.PurchaseOrders
                .AsNoTracking()
                .Where(po => !po.IsDeleted && (po.Status == PurchaseOrderStatus.PendingApproval || po.Status == PurchaseOrderStatus.Submitted));

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                q = q.Where(po =>
                    po.PurchaseOrderNumber.ToLower().Contains(term) ||
                    po.VendorName.ToLower().Contains(term) ||
                    po.ReferenceNumber.ToLower().Contains(term));
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

            var items = await q
                .OrderByDescending(po => po.SubmittedDate ?? po.CreatedAt)
                .ToListAsync(cancellationToken);

            return items.Select(po => new PurchaseOrderApprovalQueueItemDto
            {
                Id = po.Id,
                PurchaseOrderNumber = po.PurchaseOrderNumber,
                RevisionNumber = po.RevisionNumber,
                VendorName = po.VendorName,
                SubmittedBy = po.CreatedBy,
                SubmittedDate = po.SubmittedDate ?? po.CreatedAt,
                Amount = po.TotalAmount,
                Currency = po.Currency,
                Priority = po.Priority,
                Status = po.Status,
                RequiredApprovalTier = po.RequiredApprovalTier,
                CurrentApprovalTier = po.CurrentApprovalTier,
                IsExceptionApproval = po.IsExceptionApproval,
                ApprovalStatus = po.IsExceptionApproval ? "Exception Review" : $"Tier {po.RequiredApprovalTier}"
            }).ToList();
        }

        public async Task<PurchaseOrderApprovalMetricsDto> GetApprovalMetricsAsync(
            PurchaseOrderListQueryDto query,
            CancellationToken cancellationToken = default)
        {
            var baseQuery = _db.PurchaseOrders.AsNoTracking().Where(po => !po.IsDeleted);

            var pending = await baseQuery.CountAsync(po => po.Status == PurchaseOrderStatus.PendingApproval || po.Status == PurchaseOrderStatus.Submitted, cancellationToken);
            var today = DateTime.UtcNow.Date;
            var approvedToday = await baseQuery.CountAsync(po => po.Status == PurchaseOrderStatus.Approved && po.ApprovedAt >= today, cancellationToken);
            var rejected = await baseQuery.CountAsync(po => po.Status == PurchaseOrderStatus.Rejected, cancellationToken);

            return new PurchaseOrderApprovalMetricsDto
            {
                PendingApproval = pending,
                ApprovedToday = approvedToday,
                Rejected = rejected,
                RevisionRequired = 0,
                AverageApprovalTimeLabel = "1.2 hours"
            };
        }

        public async Task<PurchaseOrderDto?> ApproveAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default)
        {
            var res = await _approvalEngine.ProcessApprovalActionAsync(id, new POApprovalSignOffRequestDto
            {
                Decision = PurchaseOrderApprovalDecision.Approved,
                Comments = remarks,
                ApproverUserId = actingUser,
                ApproverName = actingUser,
                ApproverRole = "Approver"
            }, cancellationToken);

            return (await GetByIdAsync(id, cancellationToken))!;
        }

        public async Task<PurchaseOrderDto?> RejectAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default)
        {
            var res = await _approvalEngine.ProcessApprovalActionAsync(id, new POApprovalSignOffRequestDto
            {
                Decision = PurchaseOrderApprovalDecision.Rejected,
                Comments = remarks,
                ApproverUserId = actingUser,
                ApproverName = actingUser,
                ApproverRole = "Approver"
            }, cancellationToken);

            return (await GetByIdAsync(id, cancellationToken))!;
        }

        public async Task<PurchaseOrderDto?> RequestRevisionAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default)
        {
            var res = await _approvalEngine.ProcessApprovalActionAsync(id, new POApprovalSignOffRequestDto
            {
                Decision = PurchaseOrderApprovalDecision.Returned,
                Comments = remarks,
                ApproverUserId = actingUser,
                ApproverName = actingUser,
                ApproverRole = "Approver"
            }, cancellationToken);

            return (await GetByIdAsync(id, cancellationToken))!;
        }

        public async Task<PurchaseOrderDto?> ReopenAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default)
        {
            return await ChangeStatusAsync(id, PurchaseOrderStatus.Draft, remarks, actingUser, cancellationToken);
        }

        public async Task<List<PurchaseOrderApprovalHistoryDto>> GetApprovalHistoryAsync(int id, CancellationToken cancellationToken = default)
        {
            var list = await _db.PurchaseOrderApprovals
                .Where(a => a.PurchaseOrderId == id)
                .OrderByDescending(a => a.ActionedAt)
                .ToListAsync(cancellationToken);

            return list.Select(a => new PurchaseOrderApprovalHistoryDto
            {
                Id = a.Id.ToString(),
                EventKind = a.Decision.ToString(),
                Decision = a.Decision.ToString(),
                Approver = a.ApproverName,
                Role = a.ApproverRole,
                DecisionDate = a.ActionedAt ?? a.CreatedAt,
                Remarks = a.Comments ?? string.Empty,
                HmacSignatureToken = a.HmacSignatureToken
            }).ToList();
        }

        public async Task<string> GetNextNumberAsync(CancellationToken cancellationToken = default)
        {
            return await _numberingService.NextPurchaseOrderNumberAsync(cancellationToken);
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
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

            if (po is null) return null;

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
            RevisionNumber = po.RevisionNumber,
            IsCurrentRevision = po.IsCurrentRevision,
            VendorName = po.VendorName,
            VendorCode = po.VendorCode,
            ReferenceNumber = po.ReferenceNumber,
            OrderDate = po.OrderDate,
            ExpectedDeliveryDate = po.ExpectedDeliveryDate,
            Amount = po.TotalAmount,
            Currency = po.Currency,
            Status = po.Status,
            RequiredApprovalTier = po.RequiredApprovalTier,
            CurrentApprovalTier = po.CurrentApprovalTier,
            IsExceptionApproval = po.IsExceptionApproval,
            CreatedBy = po.CreatedBy,
            UpdatedAt = po.UpdatedAt,
            CreatedAt = po.CreatedAt,
            Remarks = po.Remarks
        };

        private static PurchaseOrderDto MapToDto(PurchaseOrder po) => new()
        {
            Id = po.Id,
            TenantId = po.TenantId,
            PurchaseOrderNumber = po.PurchaseOrderNumber,
            RevisionNumber = po.RevisionNumber,
            IsCurrentRevision = po.IsCurrentRevision,
            ParentPOId = po.ParentPOId,
            SupersededAt = po.SupersededAt,
            ReferenceNumber = po.ReferenceNumber,
            Vendor = new PurchaseOrderVendorDto
            {
                VendorName = po.VendorName,
                VendorCode = po.VendorCode,
                VendorContact = po.VendorContact,
                BillingAddress = po.BillingAddress,
                ShippingAddress = po.ShippingAddress,
                VendorEmail = po.VendorEmail,
                VendorPhone = po.VendorPhone,
                GstNumber = po.GstNumber,
                StateCode = po.VendorStateCode,
                Pan = po.VendorPan
            },
            VendorCode = po.VendorCode,
            VendorGstin = po.VendorGstin,
            VendorStateCode = po.VendorStateCode,
            BuyerName = po.BuyerName,
            Notes = po.Notes,
            OrderDate = po.OrderDate,
            ExpectedDeliveryDate = po.ExpectedDeliveryDate,
            PaymentTermsDays = po.PaymentTermsDays,
            PaymentTerms = po.PaymentTerms,
            Incoterms = po.Incoterms,
            DeliveryTerms = po.DeliveryTerms,
            SourceType = po.SourceType,
            SalesOrderNumber = po.SalesOrderNumber,
            Priority = po.Priority,
            Currency = po.Currency,
            ExchangeRate = po.ExchangeRate,
            Subtotal = po.Subtotal,
            DiscountTotal = po.DiscountTotal,
            TotalCgstAmount = po.TotalCgstAmount,
            TotalSgstAmount = po.TotalSgstAmount,
            TotalIgstAmount = po.TotalIgstAmount,
            TaxTotal = po.TaxTotal,
            FreightAmount = po.FreightAmount,
            InsuranceAmount = po.InsuranceAmount,
            TotalAmount = po.TotalAmount,
            GrandTotalBaseCurrency = po.GrandTotalBaseCurrency,
            Status = po.Status,
            RequiredApprovalTier = po.RequiredApprovalTier,
            CurrentApprovalTier = po.CurrentApprovalTier,
            IsExceptionApproval = po.IsExceptionApproval,
            ExceptionReasonsJson = po.ExceptionReasonsJson,
            ApprovalToken = po.ApprovalToken,
            ApprovedByUserId = po.ApprovedByUserId,
            ApprovedAt = po.ApprovedAt,
            AmendmentReason = po.AmendmentReason,
            AmendmentType = po.AmendmentType,
            Remarks = po.Remarks,
            SubmittedDate = po.SubmittedDate,
            CreatedBy = po.CreatedBy,
            CreatedDate = po.CreatedAt,
            UpdatedBy = po.UpdatedBy,
            UpdatedDate = po.UpdatedAt,
            Lines = po.Lines?.Select(l => new PurchaseOrderLineDto
            {
                Id = l.Id.ToString(),
                LineNumber = l.LineNumber,
                ItemType = l.ItemType,
                ItemCode = l.ItemCode,
                ItemName = l.ItemName,
                Description = l.Description,
                HsnCode = l.HsnCode,
                Quantity = l.Quantity,
                ReceivedQuantity = l.ReceivedQuantity,
                RejectedQuantity = l.RejectedQuantity,
                InvoicedQuantity = l.InvoicedQuantity,
                CancelledQuantity = l.CancelledQuantity,
                Unit = l.Unit,
                BaseUom = l.BaseUom,
                Rate = l.Rate,
                Discount = l.Discount,
                TaxableAmount = l.TaxableAmount,
                GstRatePercentage = l.GstRatePercentage,
                CgstAmount = l.CgstAmount,
                SgstAmount = l.SgstAmount,
                IgstAmount = l.IgstAmount,
                Tax = l.Tax,
                Amount = l.Amount,
                OverDeliveryTolerancePercent = l.OverDeliveryTolerancePercent,
                LineStatus = l.LineStatus,
                ExpectedDeliveryDate = l.ExpectedDeliveryDate,
                DeliveryScheduleJson = l.DeliveryScheduleJson
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

        private static bool IsBulkCommodity(string? unit)
        {
            if (string.IsNullOrWhiteSpace(unit)) return false;
            var u = unit.Trim().ToLowerInvariant();
            return u is "kg" or "kgs" or "ton" or "tons" or "tonne" or "tonnes" or "mtr" or "meter" or "meters" or "ltr" or "liter" or "liters" or "kl";
        }
    }
}
