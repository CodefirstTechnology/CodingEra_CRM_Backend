using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Procurement
{
    public class RequisitionConsolidationService : IRequisitionConsolidationService
    {
        private readonly ERPDbContext _db;
        private readonly RFQNumberingService _rfqNumberingService;

        public RequisitionConsolidationService(ERPDbContext db, RFQNumberingService rfqNumberingService)
        {
            _db = db;
            _rfqNumberingService = rfqNumberingService;
        }

        public async Task<List<ConsolidationCandidateLineDto>> GetConsolidationPoolAsync(int tenantId = 1, CancellationToken cancellationToken = default)
        {
            return await _db.PurchaseRequisitionLines
                .AsNoTracking()
                .Include(l => l.PurchaseRequisition)
                .Where(l => l.TenantId == tenantId &&
                            !l.PurchaseRequisition!.IsDeleted &&
                            l.PurchaseRequisition.Status == PurchaseRequisitionStatus.Approved &&
                            (l.LineStatus == PurchaseRequisitionLineStatus.PendingSourcing || (l.Quantity - l.SourcedQuantity) > 0))
                .Select(l => new ConsolidationCandidateLineDto
                {
                    Id = l.Id,
                    PurchaseRequisitionId = l.PurchaseRequisitionId,
                    PRNumber = l.PurchaseRequisition!.PRNumber,
                    Department = l.PurchaseRequisition.Department,
                    ItemId = l.ItemId,
                    ItemCode = l.ItemCode,
                    ItemName = l.ItemName,
                    Quantity = l.Quantity,
                    SourcedQuantity = l.SourcedQuantity,
                    Uom = l.Uom,
                    EstimatedPrice = l.EstimatedPrice,
                    RequiredDate = l.PurchaseRequisition.RequiredDate,
                    TargetWarehouseId = l.TargetWarehouseId,
                    IsAvlRestricted = l.IsAvlRestricted
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<RFQDto> ConsolidateLinesToRFQAsync(
            ConsolidateLinesRequestDto request,
            string actingUser,
            int tenantId = 1,
            CancellationToken cancellationToken = default)
        {
            if (request.PurchaseRequisitionLineIds == null || request.PurchaseRequisitionLineIds.Count == 0)
            {
                throw new InvalidOperationException("At least one Purchase Requisition line must be selected for consolidation.");
            }

            var prLines = await _db.PurchaseRequisitionLines
                .Include(l => l.PurchaseRequisition)
                .Where(l => l.TenantId == tenantId && request.PurchaseRequisitionLineIds.Contains(l.Id))
                .ToListAsync(cancellationToken);

            if (prLines.Count == 0)
            {
                throw new InvalidOperationException("Selected Purchase Requisition lines were not found.");
            }

            foreach (var line in prLines)
            {
                if (line.PurchaseRequisition!.Status != PurchaseRequisitionStatus.Approved)
                {
                    throw new InvalidOperationException($"Requisition '{line.PurchaseRequisition.PRNumber}' is not approved and cannot be consolidated.");
                }
            }

            string rfqNumber = await _rfqNumberingService.NextRFQNumberAsync(cancellationToken);
            var sourcingType = Enum.TryParse<RFQSourcingType>(request.SourcingType, true, out var st)
                ? st
                : RFQSourcingType.OpenCompetitive;

            var rfq = new RequestForQuotation
            {
                TenantId = tenantId,
                RFQNumber = rfqNumber,
                Title = string.IsNullOrWhiteSpace(request.Title) ? $"Consolidated Sourcing {rfqNumber}" : request.Title.Trim(),
                RFQDate = DateTime.UtcNow,
                IssueDate = DateTime.UtcNow,
                DueDate = request.BidClosingDate,
                BidClosingDate = request.BidClosingDate,
                SourcingType = sourcingType,
                IsSealedBid = request.IsSealedBid,
                Status = RFQStatus.Draft,
                DeliveryTerms = request.DeliveryTerms,
                PaymentTerms = request.PaymentTerms,
                VendorNotes = request.VendorNotes,
                CreatedBy = actingUser,
                UpdatedBy = actingUser,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Demand Grouping by (ItemId or ItemName, Uom)
            var groups = prLines.GroupBy(l => new { ItemKey = l.ItemId.HasValue ? l.ItemId.Value.ToString() : l.ItemName.Trim().ToLowerInvariant(), l.Uom });

            foreach (var grp in groups)
            {
                var sample = grp.First();
                decimal totalQty = grp.Sum(l => Math.Max(0, l.Quantity - l.SourcedQuantity));
                if (totalQty <= 0) totalQty = grp.Sum(l => l.Quantity);

                decimal maxPrice = grp.Max(l => l.EstimatedPrice);

                var rfqLine = new RequestForQuotationLine
                {
                    TenantId = tenantId,
                    ItemId = sample.ItemId,
                    ItemCode = sample.ItemCode,
                    ItemName = sample.ItemName,
                    Description = sample.Description,
                    Quantity = totalQty,
                    Uom = sample.Uom,
                    TargetUnitPrice = maxPrice > 0 ? maxPrice : null,
                    TechnicalSpecsJson = "{}"
                };

                foreach (var line in grp)
                {
                    decimal allocQty = Math.Max(0, line.Quantity - line.SourcedQuantity);
                    if (allocQty <= 0) allocQty = line.Quantity;

                    rfqLine.ConsolidatedRequisitionLines.Add(new RFQItemRequisitionLine
                    {
                        TenantId = tenantId,
                        PurchaseRequisitionLineId = line.Id,
                        AllocatedQuantity = allocQty
                    });

                    line.SourcedQuantity += allocQty;
                    line.LineStatus = PurchaseRequisitionLineStatus.RfqIssued;
                    if (line.PurchaseRequisition != null && line.PurchaseRequisition.Status == PurchaseRequisitionStatus.Approved)
                    {
                        line.PurchaseRequisition.Status = PurchaseRequisitionStatus.RfqIssued;
                        line.PurchaseRequisition.RFQNumber = rfqNumber;
                    }
                }

                rfq.Lines.Add(rfqLine);
            }

            _db.RequestForQuotations.Add(rfq);
            await _db.SaveChangesAsync(cancellationToken);

            return new RFQDto
            {
                Id = rfq.Id,
                RFQNumber = rfq.RFQNumber,
                Title = rfq.Title,
                RFQDate = rfq.RFQDate,
                DueDate = rfq.DueDate,
                BidClosingDate = rfq.BidClosingDate,
                SourcingType = rfq.SourcingType.ToString(),
                IsSealedBid = rfq.IsSealedBid,
                Status = rfq.Status,
                DeliveryTerms = rfq.DeliveryTerms,
                PaymentTerms = rfq.PaymentTerms,
                VendorNotes = rfq.VendorNotes,
                TotalLines = rfq.Lines.Count,
                TotalVendors = 0
            };
        }
    }
}
