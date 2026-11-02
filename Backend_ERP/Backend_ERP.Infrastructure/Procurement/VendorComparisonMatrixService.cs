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

namespace ERP.Infrastructure.Procurement
{
    public class VendorComparisonMatrixService : IVendorComparisonMatrixService
    {
        private readonly ERPDbContext _db;
        private readonly ILandedCostEngine _landedCostEngine;
        private readonly VendorComparisonNumberingService _comparisonNumberingService;
        private readonly PurchaseOrderNumberingService _poNumberingService;

        public VendorComparisonMatrixService(
            ERPDbContext db,
            ILandedCostEngine landedCostEngine,
            VendorComparisonNumberingService comparisonNumberingService,
            PurchaseOrderNumberingService poNumberingService)
        {
            _db = db;
            _landedCostEngine = landedCostEngine;
            _comparisonNumberingService = comparisonNumberingService;
            _poNumberingService = poNumberingService;
        }

        public async Task<VendorComparisonDto> EvaluateRfqMatrixAsync(
            int rfqId,
            string actingUser,
            int tenantId = 1,
            CancellationToken cancellationToken = default)
        {
            var rfq = await _db.RequestForQuotations
                .Include(r => r.Lines)
                .FirstOrDefaultAsync(r => r.Id == rfqId && r.TenantId == tenantId && !r.IsDeleted, cancellationToken);

            if (rfq == null)
            {
                throw new InvalidOperationException($"Request for Quotation with ID {rfqId} not found.");
            }

            // Sealed-bid viewing barrier: If sealed bid, bids cannot be evaluated before closing date
            if (rfq.IsSealedBid && DateTime.UtcNow < rfq.BidClosingDate)
            {
                throw new ProcurementSecurityException(
                    $"Sealed-bid RFQ '{rfq.RFQNumber}' cannot be evaluated prior to closing date {rfq.BidClosingDate:yyyy-MM-dd HH:mm:ss} UTC.");
            }

            // Fetch all vendor quotations submitted for this RFQ
            var quotations = await _db.VendorQuotations
                .Include(q => q.Lines)
                .Include(q => q.Vendor)
                .Where(q => q.RequestForQuotationId == rfqId && q.TenantId == tenantId && !q.IsDeleted)
                .ToListAsync(cancellationToken);

            if (quotations.Count == 0)
            {
                throw new InvalidOperationException($"No vendor quotations found for RFQ '{rfq.RFQNumber}'.");
            }

            // Find or create VendorComparison record
            var comparison = await _db.VendorComparisons
                .Include(c => c.Entries)
                .Include(c => c.ComparisonLines)
                .FirstOrDefaultAsync(c => c.RFQId == rfqId && c.TenantId == tenantId && !c.IsDeleted, cancellationToken);

            if (comparison == null)
            {
                string comparisonNumber = await _comparisonNumberingService.NextComparisonNumberAsync(cancellationToken);
                comparison = new VendorComparison
                {
                    TenantId = tenantId,
                    ComparisonNumber = comparisonNumber,
                    RFQId = rfqId,
                    RFQNumber = rfq.RFQNumber,
                    Title = $"MCDA Sourcing Comparison - {rfq.RFQNumber}",
                    ComparisonDate = DateTime.UtcNow,
                    Status = VendorComparisonStatus.UnderEvaluation,
                    EvaluatedByUserId = actingUser,
                    CreatedBy = actingUser,
                    UpdatedBy = actingUser,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.VendorComparisons.Add(comparison);
            }
            else
            {
                comparison.Status = VendorComparisonStatus.UnderEvaluation;
                comparison.EvaluatedByUserId = actingUser;
                comparison.UpdatedAt = DateTime.UtcNow;
                comparison.UpdatedBy = actingUser;
                comparison.Entries.Clear();
            }

            // Compute Landed Cost for each quotation
            foreach (var quote in quotations)
            {
                decimal quoteTotalLanded = 0m;
                foreach (var line in quote.Lines)
                {
                    var calc = _landedCostEngine.CalculateLandedCost(
                        baseUnitRate: line.UnitPrice,
                        quantity: line.Quantity,
                        discountPct: line.DiscountPercent,
                        packaging: line.PackagingAmount,
                        freight: line.FreightAmount,
                        insurance: line.InsuranceAmount,
                        gstRatePct: line.GstRatePercentage,
                        isItcEligible: line.IsItcEligible);

                    line.NetLandedCostPerUnit = calc.NetLandedCostPerUnit;
                    line.Amount = calc.TotalNetLandedCost;
                    quoteTotalLanded += calc.TotalNetLandedCost;
                }
                quote.TotalLandedCost = quoteTotalLanded;
            }

            // Gather metrics for MCDA normalization
            decimal minLandedCost = quotations.Min(q => q.TotalLandedCost > 0 ? q.TotalLandedCost : q.TotalCost);
            if (minLandedCost <= 0) minLandedCost = 1m;

            int minLeadTimeDays = quotations.Min(q => q.DeliveryTimeDays > 0 ? q.DeliveryTimeDays : 14);
            if (minLeadTimeDays <= 0) minLeadTimeDays = 1;

            var entries = new List<VendorComparisonEntry>();

            foreach (var quote in quotations)
            {
                decimal totalLanded = quote.TotalLandedCost > 0 ? quote.TotalLandedCost : quote.TotalCost;
                int leadTime = quote.DeliveryTimeDays > 0 ? quote.DeliveryTimeDays : 14;
                double qualityRating = (double)(quote.Vendor?.RatingScore ?? 75m);

                int paymentDays = 30;
                if (!string.IsNullOrWhiteSpace(quote.PaymentTerms))
                {
                    var digits = new string(quote.PaymentTerms.Where(char.IsDigit).ToArray());
                    if (int.TryParse(digits, out int pd) && pd > 0) paymentDays = pd;
                }

                // 4-Factor MCDA Scores:
                // 1. Cost Index (50%)
                double costScore = (double)(minLandedCost / (totalLanded > 0 ? totalLanded : minLandedCost)) * 100.0;
                if (costScore > 100.0) costScore = 100.0;

                // 2. Delivery Lead Time Index (25%)
                double leadTimeScore = ((double)minLeadTimeDays / leadTime) * 100.0;
                if (leadTimeScore > 100.0) leadTimeScore = 100.0;

                // 3. Quality Index (15%)
                double qualityScore = Math.Min(100.0, qualityRating);

                // 4. Payment Terms Cash-Flow Index (10%)
                double termsScore = Math.Min(100.0, (paymentDays / 60.0) * 100.0);

                // Blended MCDA Score (100% max)
                double blendedMCDAScore = Math.Round((0.50 * costScore) + (0.25 * leadTimeScore) + (0.15 * qualityScore) + (0.10 * termsScore), 2);

                bool isLowestPrice = Math.Abs(totalLanded - minLandedCost) < 0.01m;
                bool isBestDelivery = leadTime == minLeadTimeDays;

                var entry = new VendorComparisonEntry
                {
                    TenantId = tenantId,
                    VendorId = quote.VendorId,
                    VendorName = quote.VendorName,
                    QuotationRef = quote.QuotationNumber,
                    VendorQuotationId = quote.Id,
                    UnitPrice = quote.Lines.FirstOrDefault()?.UnitPrice ?? 0m,
                    TaxPercent = quote.TaxPercent,
                    DiscountPercent = quote.DiscountPercent,
                    DeliveryTimeDays = leadTime,
                    LeadTime = quote.LeadTime,
                    WarrantyPeriod = quote.WarrantyPeriod,
                    PaymentTerms = quote.PaymentTerms,
                    TotalCost = quote.TotalCost,
                    TotalLandedCost = totalLanded,
                    IsLowestPrice = isLowestPrice,
                    IsBestDelivery = isBestDelivery,
                    RecommendationScore = blendedMCDAScore,
                    LandedCostScore = Math.Round(costScore, 2),
                    LeadTimeScore = Math.Round(leadTimeScore, 2),
                    QualityScore = Math.Round(qualityScore, 2),
                    PaymentTermsScore = Math.Round(termsScore, 2),
                    BlendedMCDAScore = blendedMCDAScore,
                    Remarks = $"Cost: {costScore:F1} | Lead: {leadTimeScore:F1} | Qual: {qualityScore:F1} | Terms: {termsScore:F1}"
                };

                entries.Add(entry);
            }

            // Rank by highest BlendedMCDAScore
            var sorted = entries.OrderByDescending(e => e.BlendedMCDAScore).ThenBy(e => e.TotalLandedCost).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                sorted[i].Ranking = i + 1;
            }

            comparison.Entries = sorted;
            var winner = sorted.FirstOrDefault();
            if (winner != null)
            {
                comparison.RecommendedVendorId = winner.VendorId;
                comparison.RecommendationNotes = $"System recommendation: {winner.VendorName} ranked L1 with blended MCDA score {winner.BlendedMCDAScore} (Landed Cost: ₹{winner.TotalLandedCost:N2}, Lead Time: {winner.DeliveryTimeDays} days, Quality: {winner.QualityScore:F1}%).";
            }

            rfq.Status = RFQStatus.ComparisonReady;
            rfq.ComparisonId = comparison.Id;

            await _db.SaveChangesAsync(cancellationToken);

            return MapToDto(comparison);
        }

        public async Task<VendorComparisonDto> AwardSingleAsync(
            int comparisonId,
            VendorAwardRequestDto request,
            string actingUser,
            int tenantId = 1,
            CancellationToken cancellationToken = default)
        {
            var comparison = await _db.VendorComparisons
                .Include(c => c.Entries)
                .Include(c => c.RFQ)
                .FirstOrDefaultAsync(c => c.Id == comparisonId && c.TenantId == tenantId && !c.IsDeleted, cancellationToken);

            if (comparison == null)
            {
                throw new InvalidOperationException($"Vendor Comparison with ID {comparisonId} not found.");
            }

            var entry = comparison.Entries.FirstOrDefault(e => e.VendorId == request.VendorId);
            if (entry == null)
            {
                throw new InvalidOperationException($"Vendor with ID {request.VendorId} is not a participating bidder in this comparison.");
            }

            // 3-Quote Policy Gate: PR/RFQ > ₹1 Lakh requires >= 3 valid bids unless exempted
            decimal estimatedTotal = comparison.Entries.Max(e => e.TotalLandedCost);
            if (estimatedTotal > 100_000m && comparison.Entries.Count < 3)
            {
                if (string.IsNullOrWhiteSpace(request.SingleSourceJustificationCode))
                {
                    throw new InvalidOperationException(
                        $"Enterprise Procurement Policy Gate: RFQ award exceeding ₹1,00,000 requires at least 3 valid competitive vendor bids (found {comparison.Entries.Count}). A valid 'SingleSourceJustificationCode' (e.g. OEM_PATENTED, EMERGENCY_BREAKDOWN) must be formally recorded to grant exemption.");
                }
            }

            // Executive Override Safeguard: If user chooses non-L1, enforce justification
            if (request.VendorId != comparison.RecommendedVendorId)
            {
                if (!request.IsExecutiveOverride || string.IsNullOrWhiteSpace(request.OverrideReasonCode) || request.OverrideJustification?.Length < 20)
                {
                    throw new InvalidOperationException(
                        $"Executive Override Safeguard: Vendor '{entry.VendorName}' is not the L1 recommended bidder. Overriding the system recommendation requires setting 'IsExecutiveOverride = true', a valid reason code, and a detailed audit justification of at least 20 characters.");
                }

                comparison.IsExecutiveOverride = true;
                comparison.ExecutiveOverrideReasonCode = request.OverrideReasonCode.Trim();
                comparison.ExecutiveOverrideJustification = request.OverrideJustification.Trim();
            }

            foreach (var e in comparison.Entries)
            {
                e.IsAwardee = (e.VendorId == request.VendorId);
                e.AwardedQuantityPercentage = e.IsAwardee ? 100.00m : 0.00m;
            }

            comparison.SelectedWinnerVendorId = request.VendorId;
            comparison.SelectedWinnerVendorName = entry.VendorName;
            comparison.Status = VendorComparisonStatus.Awarded;
            comparison.IsSplitAward = false;
            comparison.SplitAllocationJson = "[]";
            comparison.UpdatedAt = DateTime.UtcNow;
            comparison.UpdatedBy = actingUser;

            if (comparison.RFQ != null)
            {
                comparison.RFQ.Status = RFQStatus.Awarded;
            }

            await _db.SaveChangesAsync(cancellationToken);

            return MapToDto(comparison);
        }

        public async Task<VendorComparisonDto> AwardSplitAsync(
            int comparisonId,
            VendorSplitAwardRequestDto request,
            string actingUser,
            int tenantId = 1,
            CancellationToken cancellationToken = default)
        {
            if (request.Allocations == null || request.Allocations.Count < 2)
            {
                throw new InvalidOperationException("Dual-sourcing split award requires at least 2 vendor allocations.");
            }

            // Validate that split percentages sum to exactly 100.00%
            decimal totalPercentage = request.Allocations.Sum(a => a.AwardedPercentage);
            if (Math.Abs(totalPercentage - 100.00m) > 0.01m)
            {
                throw new InvalidOperationException($"Split award volume percentages must sum to exactly 100.00% (currently sums to {totalPercentage:F2}%).");
            }

            var comparison = await _db.VendorComparisons
                .Include(c => c.Entries)
                .Include(c => c.RFQ)
                .FirstOrDefaultAsync(c => c.Id == comparisonId && c.TenantId == tenantId && !c.IsDeleted, cancellationToken);

            if (comparison == null)
            {
                throw new InvalidOperationException($"Vendor Comparison with ID {comparisonId} not found.");
            }

            // 3-Quote Policy Gate
            decimal estimatedTotal = comparison.Entries.Max(e => e.TotalLandedCost);
            if (estimatedTotal > 100_000m && comparison.Entries.Count < 3)
            {
                if (string.IsNullOrWhiteSpace(request.SingleSourceJustificationCode))
                {
                    throw new InvalidOperationException(
                        $"Enterprise Procurement Policy Gate: RFQ award exceeding ₹1,00,000 requires at least 3 valid competitive vendor bids (found {comparison.Entries.Count}). A valid 'SingleSourceJustificationCode' must be provided for exemption.");
                }
            }

            foreach (var e in comparison.Entries)
            {
                var alloc = request.Allocations.FirstOrDefault(a => a.VendorId == e.VendorId);
                if (alloc != null && alloc.AwardedPercentage > 0)
                {
                    e.IsAwardee = true;
                    e.AwardedQuantityPercentage = alloc.AwardedPercentage;
                }
                else
                {
                    e.IsAwardee = false;
                    e.AwardedQuantityPercentage = 0m;
                }
            }

            comparison.IsSplitAward = true;
            comparison.SplitAllocationJson = JsonSerializer.Serialize(request.Allocations);
            var primary = request.Allocations.OrderByDescending(a => a.AwardedPercentage).First();
            comparison.SelectedWinnerVendorId = primary.VendorId;
            comparison.SelectedWinnerVendorName = primary.VendorName;
            comparison.Status = VendorComparisonStatus.Awarded;
            comparison.UpdatedAt = DateTime.UtcNow;
            comparison.UpdatedBy = actingUser;

            if (comparison.RFQ != null)
            {
                comparison.RFQ.Status = RFQStatus.Awarded;
            }

            await _db.SaveChangesAsync(cancellationToken);

            return MapToDto(comparison);
        }

        public async Task<List<int>> GeneratePurchaseOrdersAsync(
            int comparisonId,
            string actingUser,
            int tenantId = 1,
            CancellationToken cancellationToken = default)
        {
            var comparison = await _db.VendorComparisons
                .Include(c => c.Entries)
                    .ThenInclude(e => e.Vendor)
                .Include(c => c.RFQ)
                    .ThenInclude(r => r!.Lines)
                        .ThenInclude(l => l.ConsolidatedRequisitionLines)
                            .ThenInclude(rl => rl.PurchaseRequisitionLine)
                .FirstOrDefaultAsync(c => c.Id == comparisonId && c.TenantId == tenantId && !c.IsDeleted, cancellationToken);

            if (comparison == null)
            {
                throw new InvalidOperationException($"Vendor Comparison with ID {comparisonId} not found.");
            }

            if (comparison.Status != VendorComparisonStatus.Awarded)
            {
                throw new InvalidOperationException("Purchase Orders can only be generated from an 'Awarded' comparison.");
            }

            var awardees = comparison.Entries.Where(e => e.IsAwardee && e.AwardedQuantityPercentage > 0).ToList();
            if (awardees.Count == 0)
            {
                throw new InvalidOperationException("No awardee vendors found in this comparison.");
            }

            var generatedPoIds = new List<int>();

            // Transactional atomicity: Generate POs and update originating PR lines
            using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (var awardee in awardees)
                {
                    string poNumber = await _poNumberingService.NextPurchaseOrderNumberAsync(cancellationToken);
                    decimal splitRatio = awardee.AwardedQuantityPercentage / 100.00m;

                    var po = new PurchaseOrder
                    {
                        PurchaseOrderNumber = poNumber,
                        OrderDate = DateTime.UtcNow,
                        VendorId = awardee.VendorId,
                        VendorName = awardee.VendorName,
                        Status = PurchaseOrderStatus.Draft,
                        PaymentTerms = awardee.PaymentTerms,
                        DeliveryTerms = comparison.RFQ?.DeliveryTerms ?? string.Empty,
                        CreatedBy = actingUser,
                        UpdatedBy = actingUser,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    decimal subTotal = 0m;
                    decimal taxAmount = 0m;

                    if (comparison.RFQ != null)
                    {
                        foreach (var rfqLine in comparison.RFQ.Lines)
                        {
                            decimal lineAllocQty = Math.Round(rfqLine.Quantity * splitRatio, 4);
                            decimal unitRate = awardee.UnitPrice > 0 ? awardee.UnitPrice : (rfqLine.TargetUnitPrice ?? 100m);
                            decimal lineAmt = Math.Round(lineAllocQty * unitRate, 2);

                            var poLine = new PurchaseOrderLine
                            {
                                ItemName = rfqLine.ItemName,
                                Description = rfqLine.Description,
                                Quantity = lineAllocQty,
                                Unit = rfqLine.Uom,
                                Rate = unitRate,
                                Amount = lineAmt
                            };
                            po.Lines.Add(poLine);

                            subTotal += lineAmt;

                            // Update originating PR lines
                            foreach (var crl in rfqLine.ConsolidatedRequisitionLines)
                            {
                                if (crl.PurchaseRequisitionLine != null)
                                {
                                    decimal prAlloc = Math.Round(crl.AllocatedQuantity * splitRatio, 4);
                                    crl.PurchaseRequisitionLine.OrderedQuantity += prAlloc;
                                    if (crl.PurchaseRequisitionLine.OrderedQuantity >= crl.PurchaseRequisitionLine.Quantity)
                                    {
                                        crl.PurchaseRequisitionLine.LineStatus = PurchaseRequisitionLineStatus.FullyOrdered;
                                    }
                                    else if (crl.PurchaseRequisitionLine.OrderedQuantity > 0)
                                    {
                                        crl.PurchaseRequisitionLine.LineStatus = PurchaseRequisitionLineStatus.PartiallyOrdered;
                                    }
                                }
                            }
                        }
                    }

                    po.Subtotal = subTotal;
                    po.TaxTotal = Math.Round(subTotal * (awardee.TaxPercent / 100m), 2);
                    po.TotalAmount = po.Subtotal + po.TaxTotal;

                    _db.PurchaseOrders.Add(po);
                    await _db.SaveChangesAsync(cancellationToken);

                    generatedPoIds.Add(po.Id);
                    if (generatedPoIds.Count == 1)
                    {
                        comparison.PurchaseOrderId = po.Id;
                        comparison.PurchaseOrderNumber = po.PurchaseOrderNumber;
                    }
                }

                await _db.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }

            return generatedPoIds;
        }

        private static VendorComparisonDto MapToDto(VendorComparison c)
        {
            return new VendorComparisonDto
            {
                Id = c.Id,
                ComparisonNumber = c.ComparisonNumber,
                RFQId = c.RFQId,
                RFQNumber = c.RFQNumber,
                Title = c.Title,
                ComparisonDate = c.ComparisonDate,
                RecommendationNotes = c.RecommendationNotes,
                RecommendedVendorId = c.RecommendedVendorId,
                SelectedWinnerVendorId = c.SelectedWinnerVendorId,
                SelectedWinnerVendorName = c.SelectedWinnerVendorName,
                PurchaseOrderId = c.PurchaseOrderId,
                PurchaseOrderNumber = c.PurchaseOrderNumber,
                IsExecutiveOverride = c.IsExecutiveOverride,
                ExecutiveOverrideReasonCode = c.ExecutiveOverrideReasonCode,
                ExecutiveOverrideJustification = c.ExecutiveOverrideJustification,
                IsSplitAward = c.IsSplitAward,
                SplitAllocationJson = c.SplitAllocationJson,
                EvaluatedByUserId = c.EvaluatedByUserId,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                CreatedBy = c.CreatedBy,
                UpdatedAt = c.UpdatedAt,
                UpdatedBy = c.UpdatedBy,
                Vendors = c.Entries.Select(e => new VendorQuotationEntryDto
                {
                    Id = e.Id,
                    VendorComparisonId = e.VendorComparisonId,
                    VendorId = e.VendorId,
                    VendorName = e.VendorName,
                    QuotationRef = e.QuotationRef,
                    VendorQuotationId = e.VendorQuotationId,
                    UnitPrice = e.UnitPrice,
                    TaxPercent = e.TaxPercent,
                    DiscountPercent = e.DiscountPercent,
                    DeliveryTimeDays = e.DeliveryTimeDays,
                    LeadTime = e.LeadTime,
                    WarrantyPeriod = e.WarrantyPeriod,
                    PaymentTerms = e.PaymentTerms,
                    TotalCost = e.TotalCost,
                    TotalLandedCost = e.TotalLandedCost,
                    Ranking = e.Ranking,
                    IsLowestPrice = e.IsLowestPrice,
                    IsBestDelivery = e.IsBestDelivery,
                    RecommendationScore = e.RecommendationScore,
                    LandedCostScore = e.LandedCostScore,
                    LeadTimeScore = e.LeadTimeScore,
                    QualityScore = e.QualityScore,
                    PaymentTermsScore = e.PaymentTermsScore,
                    BlendedMCDAScore = e.BlendedMCDAScore,
                    IsAwardee = e.IsAwardee,
                    AwardedQuantityPercentage = e.AwardedQuantityPercentage,
                    Remarks = e.Remarks
                }).ToList()
            };
        }
    }
}
