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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Infrastructure.Procurement
{
    public class PurchaseOrderAmendmentService : IPurchaseOrderAmendmentService
    {
        private readonly ERPDbContext _dbContext;
        private readonly IPurchaseOrderApprovalEngine _approvalEngine;
        private readonly ILogger<PurchaseOrderAmendmentService> _logger;

        public PurchaseOrderAmendmentService(
            ERPDbContext dbContext,
            IPurchaseOrderApprovalEngine approvalEngine,
            ILogger<PurchaseOrderAmendmentService> logger)
        {
            _dbContext = dbContext;
            _approvalEngine = approvalEngine;
            _logger = logger;
        }

        public async Task<PurchaseOrder> AmendPurchaseOrderAsync(
            int predecessorPoId,
            POAmendmentRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.AmendmentReason))
            {
                throw new ArgumentException("An explicit amendment reason is mandatory for spawning a PO revision.", nameof(request.AmendmentReason));
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // 1. Acquire pessimistic lock on predecessor to ensure deadlock-free revision spawning
                var predecessor = await _dbContext.PurchaseOrders
                    .Include(p => p.Lines)
                    .Include(p => p.Vendor)
                    .FirstOrDefaultAsync(p => p.Id == predecessorPoId, cancellationToken);

                if (predecessor == null)
                {
                    throw new KeyNotFoundException($"Predecessor Purchase Order ID {predecessorPoId} was not found.");
                }

                if (!predecessor.IsCurrentRevision)
                {
                    throw new InvalidOperationException($"Cannot amend PO revision R{predecessor.RevisionNumber} because it is not the current active revision.");
                }

                if (predecessor.Status is PurchaseOrderStatus.Cancelled or PurchaseOrderStatus.Closed or PurchaseOrderStatus.Superseded)
                {
                    throw new InvalidOperationException($"Cannot amend Purchase Order in terminal status '{predecessor.Status}'.");
                }

                var now = DateTime.UtcNow;

                // 2. Mark predecessor as superseded and freeze it
                predecessor.IsCurrentRevision = false;
                predecessor.Status = PurchaseOrderStatus.Superseded;
                predecessor.SupersededAt = now;
                predecessor.UpdatedAt = now;
                predecessor.UpdatedBy = actingUser;

                // 3. Spawn Revision R+1
                var newRevision = new PurchaseOrder
                {
                    TenantId = predecessor.TenantId,
                    PurchaseOrderNumber = predecessor.PurchaseOrderNumber,
                    RevisionNumber = predecessor.RevisionNumber + 1,
                    ParentPOId = predecessor.Id,
                    IsCurrentRevision = true,
                    ReferenceNumber = predecessor.ReferenceNumber,

                    // Vendor Snapshot Preservation
                    VendorId = predecessor.VendorId,
                    VendorCode = predecessor.VendorCode,
                    VendorName = predecessor.VendorName,
                    VendorGstin = predecessor.VendorGstin,
                    VendorStateCode = predecessor.VendorStateCode,
                    VendorPan = predecessor.VendorPan,
                    VendorContact = predecessor.VendorContact,
                    VendorEmail = predecessor.VendorEmail,
                    VendorPhone = predecessor.VendorPhone,
                    BillingAddress = predecessor.BillingAddress,
                    ShippingAddress = predecessor.ShippingAddress,
                    BuyerName = predecessor.BuyerName,

                    // Dates and Commercial Terms
                    OrderDate = predecessor.OrderDate,
                    ExpectedDeliveryDate = predecessor.ExpectedDeliveryDate,
                    Currency = predecessor.Currency,
                    ExchangeRate = request.ExchangeRate.HasValue && request.ExchangeRate.Value > 0 ? request.ExchangeRate.Value : predecessor.ExchangeRate,
                    PaymentTermsDays = request.PaymentTermsDays ?? predecessor.PaymentTermsDays,
                    PaymentTerms = request.PaymentTerms ?? predecessor.PaymentTerms,
                    Incoterms = request.Incoterms ?? predecessor.Incoterms,
                    DeliveryTerms = request.DeliveryTerms ?? predecessor.DeliveryTerms,
                    Notes = request.Notes ?? predecessor.Notes,
                    Remarks = request.Remarks ?? predecessor.Remarks,

                    // Lineage & Metadata
                    Priority = predecessor.Priority,
                    SourceType = predecessor.SourceType,
                    SalesOrderNumber = predecessor.SalesOrderNumber,
                    PurchaseRequisitionId = predecessor.PurchaseRequisitionId,
                    RFQId = predecessor.RFQId,
                    VendorComparisonId = predecessor.VendorComparisonId,

                    // Amendment Metadata
                    AmendmentReason = request.AmendmentReason,
                    AmendmentType = request.AmendmentType,
                    Status = PurchaseOrderStatus.PendingApproval,
                    CurrentApprovalTier = PurchaseOrderApprovalTier.None,
                    CreatedAt = now,
                    CreatedBy = actingUser,
                    UpdatedAt = now,
                    UpdatedBy = actingUser
                };

                // 4. Map Lines: either use modified lines from request or clone predecessor lines
                var linesToProcess = request.Lines != null && request.Lines.Count > 0 
                    ? request.Lines 
                    : predecessor.Lines.Select(l => new EnterprisePOLineInputDto
                    {
                        LineNumber = l.LineNumber,
                        ItemType = l.ItemType,
                        ItemCode = l.ItemCode,
                        ItemName = l.ItemName,
                        Description = l.Description,
                        HsnCode = l.HsnCode,
                        Unit = l.Unit,
                        Quantity = l.Quantity,
                        Rate = l.Rate,
                        DiscountPercent = l.Discount,
                        GstRatePercentage = l.GstRatePercentage,
                        OverDeliveryTolerancePercent = l.OverDeliveryTolerancePercent,
                        ExpectedDeliveryDate = l.ExpectedDeliveryDate,
                        TargetWarehouseId = l.TargetWarehouseId,
                        PurchaseRequisitionLineId = l.PurchaseRequisitionLineId,
                        RFQLineId = l.RFQLineId,
                        VendorQuotationLineId = l.VendorQuotationLineId
                    }).ToList();

                decimal subtotal = 0m;
                decimal discountTotal = 0m;
                decimal totalCgst = 0m;
                decimal totalSgst = 0m;
                decimal totalIgst = 0m;
                decimal taxTotal = 0m;

                bool isInterState = !string.IsNullOrWhiteSpace(predecessor.VendorStateCode) && 
                                    !string.Equals(predecessor.VendorStateCode, "27", StringComparison.OrdinalIgnoreCase);

                int lineNum = 1;
                foreach (var lineDto in linesToProcess)
                {
                    decimal validatedTolerance = lineDto.OverDeliveryTolerancePercent;
                    bool isBulk = IsBulkCommodity(lineDto.Unit);
                    if (!isBulk)
                    {
                        validatedTolerance = 0.00m;
                    }
                    else if (validatedTolerance > 2.00m)
                    {
                        validatedTolerance = 2.00m;
                    }

                    if (lineDto.DeliverySchedules != null && lineDto.DeliverySchedules.Count > 0)
                    {
                        decimal scheduleSum = lineDto.DeliverySchedules.Sum(s => s.Quantity);
                        if (Math.Abs(scheduleSum - lineDto.Quantity) > 0.0001m)
                        {
                            throw new InvalidOperationException($"Line {lineNum} ({lineDto.ItemName}): Sum of delivery schedules ({scheduleSum}) must equal ordered quantity ({lineDto.Quantity}).");
                        }
                    }

                    decimal lineGross = Math.Round(lineDto.Quantity * lineDto.Rate, 2, MidpointRounding.AwayFromZero);
                    decimal lineDiscount = Math.Round(lineGross * (lineDto.DiscountPercent / 100m), 2, MidpointRounding.AwayFromZero);
                    decimal lineTaxable = lineGross - lineDiscount;

                    decimal lineCgst = 0m;
                    decimal lineSgst = 0m;
                    decimal lineIgst = 0m;

                    if (isInterState)
                    {
                        lineIgst = Math.Round(lineTaxable * (lineDto.GstRatePercentage / 100m), 2, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        decimal halfRate = lineDto.GstRatePercentage / 2m;
                        lineCgst = Math.Round(lineTaxable * (halfRate / 100m), 2, MidpointRounding.AwayFromZero);
                        lineSgst = Math.Round(lineTaxable * (halfRate / 100m), 2, MidpointRounding.AwayFromZero);
                    }

                    decimal lineTax = lineCgst + lineSgst + lineIgst;
                    decimal lineTotal = lineTaxable + lineTax;

                    var poLine = new PurchaseOrderLine
                    {
                        TenantId = predecessor.TenantId,
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
                        DiscountAmount = lineDiscount,
                        TaxableAmount = lineTaxable,
                        GstRatePercentage = lineDto.GstRatePercentage,
                        CgstAmount = lineCgst,
                        SgstAmount = lineSgst,
                        IgstAmount = lineIgst,
                        Tax = lineTax,
                        TaxAmount = lineTax,
                        Amount = lineTotal,
                        LineTotal = lineTotal,
                        OverDeliveryTolerancePercent = validatedTolerance,
                        ExpectedDeliveryDate = lineDto.ExpectedDeliveryDate ?? newRevision.ExpectedDeliveryDate,
                        TargetWarehouseId = lineDto.TargetWarehouseId,
                        PurchaseRequisitionLineId = lineDto.PurchaseRequisitionLineId,
                        RFQLineId = lineDto.RFQLineId,
                        VendorQuotationLineId = lineDto.VendorQuotationLineId,
                        DeliveryScheduleJson = lineDto.DeliverySchedules != null && lineDto.DeliverySchedules.Count > 0
                            ? JsonSerializer.Serialize(lineDto.DeliverySchedules)
                            : "[]",
                        LineStatus = PurchaseOrderLineStatus.Pending
                    };

                    newRevision.Lines.Add(poLine);

                    subtotal += lineTaxable;
                    discountTotal += lineDiscount;
                    totalCgst += lineCgst;
                    totalSgst += lineSgst;
                    totalIgst += lineIgst;
                    taxTotal += lineTax;
                }

                newRevision.Subtotal = subtotal;
                newRevision.DiscountTotal = discountTotal;
                newRevision.TotalCgstAmount = totalCgst;
                newRevision.TotalSgstAmount = totalSgst;
                newRevision.TotalIgstAmount = totalIgst;
                newRevision.TaxTotal = taxTotal;
                newRevision.TotalAmount = subtotal + taxTotal + newRevision.FreightAmount + newRevision.InsuranceAmount;
                newRevision.GrandTotalBaseCurrency = Math.Round(newRevision.TotalAmount * newRevision.ExchangeRate, 2, MidpointRounding.AwayFromZero);

                // 5. Evaluate Delegation of Authority (DOA) Requirements for R+1
                decimal? vendorRating = predecessor.Vendor?.RatingScore;
                var (requiredTier, isException, exceptionReasons) = await _approvalEngine.EvaluateDOARequirementsAsync(
                    newRevision,
                    vendorRating,
                    false,
                    false,
                    cancellationToken);

                newRevision.RequiredApprovalTier = requiredTier;
                newRevision.IsExceptionApproval = isException;
                newRevision.ExceptionReasonsJson = JsonSerializer.Serialize(exceptionReasons);

                _dbContext.PurchaseOrders.Add(newRevision);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // 6. Record Audit Delta Records in PurchaseOrderAmendmentHistory
                var deltas = new List<PurchaseOrderAmendmentHistory>();

                if (predecessor.TotalAmount != newRevision.TotalAmount)
                {
                    deltas.Add(new PurchaseOrderAmendmentHistory
                    {
                        TenantId = predecessor.TenantId,
                        PurchaseOrderId = newRevision.Id,
                        RevisionNumber = newRevision.RevisionNumber,
                        ChangedField = "TotalAmount",
                        OldValue = predecessor.TotalAmount.ToString("F2"),
                        NewValue = newRevision.TotalAmount.ToString("F2"),
                        AmendmentReason = request.AmendmentReason,
                        ActionedByUserId = actingUser,
                        CreatedAt = now
                    });
                }

                if (predecessor.PaymentTermsDays != newRevision.PaymentTermsDays)
                {
                    deltas.Add(new PurchaseOrderAmendmentHistory
                    {
                        TenantId = predecessor.TenantId,
                        PurchaseOrderId = newRevision.Id,
                        RevisionNumber = newRevision.RevisionNumber,
                        ChangedField = "PaymentTermsDays",
                        OldValue = predecessor.PaymentTermsDays.ToString(),
                        NewValue = newRevision.PaymentTermsDays.ToString(),
                        AmendmentReason = request.AmendmentReason,
                        ActionedByUserId = actingUser,
                        CreatedAt = now
                    });
                }

                if (predecessor.ExchangeRate != newRevision.ExchangeRate)
                {
                    deltas.Add(new PurchaseOrderAmendmentHistory
                    {
                        TenantId = predecessor.TenantId,
                        PurchaseOrderId = newRevision.Id,
                        RevisionNumber = newRevision.RevisionNumber,
                        ChangedField = "ExchangeRate",
                        OldValue = predecessor.ExchangeRate.ToString("F6"),
                        NewValue = newRevision.ExchangeRate.ToString("F6"),
                        AmendmentReason = request.AmendmentReason,
                        ActionedByUserId = actingUser,
                        CreatedAt = now
                    });
                }

                if (deltas.Count == 0)
                {
                    deltas.Add(new PurchaseOrderAmendmentHistory
                    {
                        TenantId = predecessor.TenantId,
                        PurchaseOrderId = newRevision.Id,
                        RevisionNumber = newRevision.RevisionNumber,
                        ChangedField = "LinesReconfigured",
                        OldValue = $"{predecessor.Lines.Count} line(s)",
                        NewValue = $"{newRevision.Lines.Count} line(s)",
                        AmendmentReason = request.AmendmentReason,
                        ActionedByUserId = actingUser,
                        CreatedAt = now
                    });
                }

                _dbContext.PurchaseOrderAmendmentHistories.AddRange(deltas);
                await _dbContext.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Spawned PO Revision R{Revision} from Predecessor {PredecessorId} for PO {PONumber}",
                    newRevision.RevisionNumber, predecessor.Id, newRevision.PurchaseOrderNumber);

                return newRevision;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to amend Purchase Order {PredecessorId}", predecessorPoId);
                throw;
            }
        }

        public async Task<List<PurchaseOrderRevisionSummaryDto>> GetRevisionHistoryAsync(int poId, CancellationToken cancellationToken = default)
        {
            var targetPo = await _dbContext.PurchaseOrders
                .Where(p => p.Id == poId)
                .Select(p => new { p.TenantId, p.PurchaseOrderNumber })
                .FirstOrDefaultAsync(cancellationToken);

            if (targetPo == null)
            {
                return new List<PurchaseOrderRevisionSummaryDto>();
            }

            return await _dbContext.PurchaseOrders
                .Where(p => p.TenantId == targetPo.TenantId && p.PurchaseOrderNumber == targetPo.PurchaseOrderNumber)
                .OrderByDescending(p => p.RevisionNumber)
                .Select(p => new PurchaseOrderRevisionSummaryDto
                {
                    Id = p.Id,
                    PurchaseOrderNumber = p.PurchaseOrderNumber,
                    RevisionNumber = p.RevisionNumber,
                    IsCurrentRevision = p.IsCurrentRevision,
                    Status = p.Status,
                    TotalAmount = p.TotalAmount,
                    Currency = p.Currency,
                    AmendmentReason = p.AmendmentReason,
                    AmendmentType = p.AmendmentType != null ? p.AmendmentType.ToString() : null,
                    CreatedAt = p.CreatedAt,
                    SupersededAt = p.SupersededAt,
                    CreatedBy = p.CreatedBy
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<PurchaseOrderAmendmentDeltaDto>> GetAmendmentAuditTrailAsync(int poId, CancellationToken cancellationToken = default)
        {
            var targetPo = await _dbContext.PurchaseOrders
                .Where(p => p.Id == poId)
                .Select(p => new { p.TenantId, p.PurchaseOrderNumber })
                .FirstOrDefaultAsync(cancellationToken);

            if (targetPo == null)
            {
                return new List<PurchaseOrderAmendmentDeltaDto>();
            }

            var poIds = await _dbContext.PurchaseOrders
                .Where(p => p.TenantId == targetPo.TenantId && p.PurchaseOrderNumber == targetPo.PurchaseOrderNumber)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            return await _dbContext.PurchaseOrderAmendmentHistories
                .Where(h => poIds.Contains(h.PurchaseOrderId))
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => new PurchaseOrderAmendmentDeltaDto
                {
                    Id = h.Id,
                    PurchaseOrderId = h.PurchaseOrderId,
                    RevisionNumber = h.RevisionNumber,
                    ChangedField = h.ChangedField,
                    OldValue = h.OldValue,
                    NewValue = h.NewValue,
                    AmendmentReason = h.AmendmentReason,
                    ActionedByUserId = h.ActionedByUserId,
                    CreatedAt = h.CreatedAt
                })
                .ToListAsync(cancellationToken);
        }

        private static bool IsBulkCommodity(string? unit)
        {
            if (string.IsNullOrWhiteSpace(unit)) return false;
            var u = unit.Trim().ToLowerInvariant();
            return u is "kg" or "kgs" or "ton" or "tons" or "tonne" or "tonnes" or "mtr" or "meter" or "meters" or "ltr" or "liter" or "liters" or "kl";
        }
    }
}
