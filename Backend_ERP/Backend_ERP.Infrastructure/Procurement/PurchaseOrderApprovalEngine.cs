using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
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
    public class PurchaseOrderApprovalEngine : IPurchaseOrderApprovalEngine
    {
        private const string SecretKey = "Buildrich_Enterprise_PO_HMAC_Secret_Key_2026";
        private readonly ERPDbContext _dbContext;
        private readonly ILogger<PurchaseOrderApprovalEngine> _logger;

        public PurchaseOrderApprovalEngine(ERPDbContext dbContext, ILogger<PurchaseOrderApprovalEngine> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<(PurchaseOrderApprovalTier RequiredTier, bool IsException, List<string> ExceptionReasons)> EvaluateDOARequirementsAsync(
            PurchaseOrder po, 
            decimal? vendorRatingScore = null, 
            bool isUnbudgetedCapEx = false,
            bool isNonAvlEmergencyOrder = false,
            CancellationToken cancellationToken = default)
        {
            var reasons = new List<string>();
            bool isException = false;

            // 1. Monetary Base DOA Tiers (GrandTotalBaseCurrency)
            decimal amount = po.GrandTotalBaseCurrency > 0 ? po.GrandTotalBaseCurrency : po.TotalAmount;
            PurchaseOrderApprovalTier tier;

            if (amount <= 100_000m)
            {
                tier = PurchaseOrderApprovalTier.Tier1; // Purchase Manager
            }
            else if (amount <= 1_000_000m)
            {
                tier = PurchaseOrderApprovalTier.Tier2; // Head of Procurement
            }
            else if (amount <= 5_000_000m)
            {
                tier = PurchaseOrderApprovalTier.Tier3; // VP Supply Chain
            }
            else
            {
                tier = PurchaseOrderApprovalTier.Tier4; // CFO & Managing Director
            }

            // 2. Exception Trigger 1: Unbudgeted CapEx expenditure mandates CFO & MD sign-off
            if (isUnbudgetedCapEx)
            {
                tier = PurchaseOrderApprovalTier.Tier4;
                isException = true;
                reasons.Add("Unbudgeted CapEx expenditure mandates CFO & Managing Director sign-off (Tier 4)");
            }

            // 3. Exception Trigger 2: Payment Terms > 45 days (MSME Section 43B(h) compliance)
            if (po.PaymentTermsDays > 45)
            {
                tier = PurchaseOrderApprovalTier.Tier4;
                isException = true;
                reasons.Add($"Payment terms exceed 45 days ({po.PaymentTermsDays} days), triggering MSME Section 43B(h) compliance escalation to CFO (Tier 4)");
            }

            // 4. Exception Trigger 3: Low Vendor Rating Score (< 70.00)
            if (vendorRatingScore.HasValue && vendorRatingScore.Value < 70.00m)
            {
                if (tier < PurchaseOrderApprovalTier.Tier3)
                {
                    tier = PurchaseOrderApprovalTier.Tier3;
                }
                isException = true;
                reasons.Add($"Vendor performance rating score is critically low ({vendorRatingScore.Value:F2} / 100.00), escalating order to VP Supply Chain (Tier 3)");
            }

            // 5. Exception Trigger 4: Non-AVL Emergency Order
            if (isNonAvlEmergencyOrder)
            {
                if (tier < PurchaseOrderApprovalTier.Tier3)
                {
                    tier = PurchaseOrderApprovalTier.Tier3;
                }
                isException = true;
                reasons.Add("Emergency procurement placed with non-AVL vendor requires VP Supply Chain sign-off (Tier 3)");
            }

            return await Task.FromResult((tier, isException, reasons));
        }

        public string GenerateHmacSignature(int tenantId, string poNumber, int revisionNumber, decimal totalAmount, string approverUserId, DateTime actionedAt)
        {
            string payload = $"{tenantId}|{poNumber}|{revisionNumber}|{totalAmount:F2}|{approverUserId}|{actionedAt:yyyyMMddHHmmss}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hash);
        }

        public bool VerifyHmacSignature(string token, int tenantId, string poNumber, int revisionNumber, decimal totalAmount, string approverUserId, DateTime actionedAt)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            string expected = GenerateHmacSignature(tenantId, poNumber, revisionNumber, totalAmount, approverUserId, actionedAt);
            byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
            byte[] tokenBytes = Encoding.UTF8.GetBytes(token);
            return CryptographicOperations.FixedTimeEquals(expectedBytes, tokenBytes);
        }

        public async Task<POApprovalSignOffResultDto> ProcessApprovalActionAsync(int poId, POApprovalSignOffRequestDto request, CancellationToken cancellationToken = default)
        {
            var po = await _dbContext.PurchaseOrders
                .Include(p => p.Approvals)
                .FirstOrDefaultAsync(p => p.Id == poId, cancellationToken);

            if (po == null)
            {
                return new POApprovalSignOffResultDto
                {
                    IsSuccess = false,
                    PurchaseOrderId = poId,
                    Message = $"Purchase order ID {poId} was not found."
                };
            }

            if (po.Status != PurchaseOrderStatus.PendingApproval && po.Status != PurchaseOrderStatus.Submitted)
            {
                return new POApprovalSignOffResultDto
                {
                    IsSuccess = false,
                    PurchaseOrderId = poId,
                    PurchaseOrderNumber = po.PurchaseOrderNumber,
                    NewStatus = po.Status,
                    Message = $"Purchase order is in status '{po.Status}', cannot process approval actions."
                };
            }

            var now = DateTime.UtcNow;
            var approverTier = ResolveTierFromRole(request.ApproverRole);

            if (request.Decision == PurchaseOrderApprovalDecision.Approved)
            {
                if (approverTier < po.CurrentApprovalTier && po.CurrentApprovalTier > 0)
                {
                    return new POApprovalSignOffResultDto
                    {
                        IsSuccess = false,
                        PurchaseOrderId = poId,
                        PurchaseOrderNumber = po.PurchaseOrderNumber,
                        CurrentTier = po.CurrentApprovalTier,
                        RequiredTier = po.RequiredApprovalTier,
                        Message = $"Approver tier {approverTier} is below current required progression tier {po.CurrentApprovalTier}."
                    };
                }

                string signature = GenerateHmacSignature(
                    po.TenantId, 
                    po.PurchaseOrderNumber, 
                    po.RevisionNumber, 
                    po.TotalAmount, 
                    request.ApproverUserId, 
                    now);

                var approvalRecord = new PurchaseOrderApproval
                {
                    TenantId = po.TenantId,
                    PurchaseOrderId = po.Id,
                    TierLevel = approverTier,
                    ApproverRole = request.ApproverRole,
                    ApproverUserId = request.ApproverUserId,
                    ApproverName = request.ApproverName,
                    Decision = request.Decision,
                    Comments = request.Comments,
                    HmacSignatureToken = signature,
                    IsExceptionApproval = po.IsExceptionApproval,
                    ActionedAt = now,
                    CreatedAt = now
                };

                po.Approvals.Add(approvalRecord);
                po.CurrentApprovalTier = approverTier;

                // Check if final required tier is achieved
                if (po.CurrentApprovalTier >= po.RequiredApprovalTier)
                {
                    po.Status = PurchaseOrderStatus.Approved;
                    po.ApprovedAt = now;
                    po.ApprovedByUserId = request.ApproverUserId;
                    po.ApprovalToken = signature;
                }
                else
                {
                    // Advance to next required tier
                    po.CurrentApprovalTier = (PurchaseOrderApprovalTier)((int)po.CurrentApprovalTier + 1);
                }

                po.UpdatedAt = now;
                po.UpdatedBy = request.ApproverUserId;

                await _dbContext.SaveChangesAsync(cancellationToken);

                return new POApprovalSignOffResultDto
                {
                    IsSuccess = true,
                    PurchaseOrderId = po.Id,
                    PurchaseOrderNumber = po.PurchaseOrderNumber,
                    NewStatus = po.Status,
                    CurrentTier = po.CurrentApprovalTier,
                    RequiredTier = po.RequiredApprovalTier,
                    HmacSignatureToken = signature,
                    Message = po.Status == PurchaseOrderStatus.Approved 
                        ? "Purchase order has been fully authorized with cryptographic sign-off." 
                        : $"Tier {approverTier} approval signed off. Progressing to Tier {po.CurrentApprovalTier}."
                };
            }
            else if (request.Decision == PurchaseOrderApprovalDecision.Rejected)
            {
                var rejectionRecord = new PurchaseOrderApproval
                {
                    TenantId = po.TenantId,
                    PurchaseOrderId = po.Id,
                    TierLevel = approverTier,
                    ApproverRole = request.ApproverRole,
                    ApproverUserId = request.ApproverUserId,
                    ApproverName = request.ApproverName,
                    Decision = request.Decision,
                    Comments = request.Comments,
                    ActionedAt = now,
                    CreatedAt = now
                };

                po.Approvals.Add(rejectionRecord);
                po.Status = PurchaseOrderStatus.Rejected;
                po.UpdatedAt = now;
                po.UpdatedBy = request.ApproverUserId;

                await _dbContext.SaveChangesAsync(cancellationToken);

                return new POApprovalSignOffResultDto
                {
                    IsSuccess = true,
                    PurchaseOrderId = po.Id,
                    PurchaseOrderNumber = po.PurchaseOrderNumber,
                    NewStatus = po.Status,
                    CurrentTier = po.CurrentApprovalTier,
                    RequiredTier = po.RequiredApprovalTier,
                    Message = "Purchase order has been rejected."
                };
            }
            else // Returned / RequestRevision
            {
                var returnRecord = new PurchaseOrderApproval
                {
                    TenantId = po.TenantId,
                    PurchaseOrderId = po.Id,
                    TierLevel = approverTier,
                    ApproverRole = request.ApproverRole,
                    ApproverUserId = request.ApproverUserId,
                    ApproverName = request.ApproverName,
                    Decision = PurchaseOrderApprovalDecision.Returned,
                    Comments = request.Comments,
                    ActionedAt = now,
                    CreatedAt = now
                };

                po.Approvals.Add(returnRecord);
                po.Status = PurchaseOrderStatus.Draft;
                po.UpdatedAt = now;
                po.UpdatedBy = request.ApproverUserId;

                await _dbContext.SaveChangesAsync(cancellationToken);

                return new POApprovalSignOffResultDto
                {
                    IsSuccess = true,
                    PurchaseOrderId = po.Id,
                    PurchaseOrderNumber = po.PurchaseOrderNumber,
                    NewStatus = po.Status,
                    CurrentTier = po.CurrentApprovalTier,
                    RequiredTier = po.RequiredApprovalTier,
                    Message = "Purchase order has been returned to Draft for required revisions."
                };
            }
        }

        private static PurchaseOrderApprovalTier ResolveTierFromRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role)) return PurchaseOrderApprovalTier.Tier1;

            var r = role.Trim().ToLowerInvariant();
            if (r.Contains("cfo") || r.Contains("director") || r.Contains("md") || r.Contains("board") || r.Contains("admin"))
            {
                return PurchaseOrderApprovalTier.Tier4;
            }
            if (r.Contains("vp") || r.Contains("vpsupplychain") || r.Contains("supplychain"))
            {
                return PurchaseOrderApprovalTier.Tier3;
            }
            if (r.Contains("head") || r.Contains("procurementhead"))
            {
                return PurchaseOrderApprovalTier.Tier2;
            }

            return PurchaseOrderApprovalTier.Tier1;
        }
    }
}

