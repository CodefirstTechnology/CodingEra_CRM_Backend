using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ERP.Infrastructure.Sales
{
    public class ProformaApprovalService : IProformaApprovalService
    {
        private readonly ERPDbContext _db;
        private readonly string _hmacSecret;

        public ProformaApprovalService(ERPDbContext db, IConfiguration configuration)
        {
            _db = db;
            _hmacSecret = configuration["Security:ApprovalSecretKey"] ?? "ERP_ENTERPRISE_PROFORMA_SIGNATURE_KEY_2026_HARDENED!";
        }

        public async Task<bool> RequiresApprovalAsync(int proformaInvoiceId, CancellationToken ct = default)
        {
            var pi = await _db.ProformaInvoices
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == proformaInvoiceId, ct);

            if (pi == null) return false;

            if (pi.GrandTotal > 500000m) return true;
            if (string.Equals(pi.BillingType, "MilestonePercentage", StringComparison.OrdinalIgnoreCase) &&
                pi.MilestonePercentage.HasValue && pi.MilestonePercentage.Value < 30m) return true;
            if (pi.RequiresFinanceCreditReview) return true;

            return false;
        }

        public string GenerateHmacToken(int proformaInvoiceId, string userId, string decision, string tenantId = "default")
        {
            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
            var raw = $"{tenantId}:{proformaInvoiceId}:{userId}:{decision}:{timestamp}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_hmacSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return $"{Convert.ToHexString(hash)}.{timestamp}";
        }

        public bool VerifyHmacToken(int proformaInvoiceId, string userId, string decision, string token, string tenantId = "default")
        {
            if (string.IsNullOrWhiteSpace(token) || !token.Contains('.')) return false;

            var parts = token.Split('.');
            if (parts.Length != 2) return false;

            var providedHash = parts[0];
            var timestamp = parts[1];

            var raw = $"{tenantId}:{proformaInvoiceId}:{userId}:{decision}:{timestamp}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_hmacSecret));
            var computedHash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(raw)));

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(providedHash),
                Encoding.UTF8.GetBytes(computedHash));
        }

        public async Task<ProformaInvoiceDto> ProcessDecisionAsync(
            int proformaInvoiceId,
            ProformaInvoiceApprovalRequestDto request,
            string actingUser,
            string? ipAddress = null,
            CancellationToken ct = default)
        {
            var pi = await _db.ProformaInvoices
                .Include(x => x.Items)
                .Include(x => x.StatusHistory)
                .Include(x => x.ApprovalHistory)
                .Include(x => x.SettlementAllocations)
                .FirstOrDefaultAsync(x => x.Id == proformaInvoiceId, ct);

            if (pi == null)
            {
                throw new InvalidOperationException($"Proforma Invoice #{proformaInvoiceId} not found.");
            }

            var decision = (request.Decision ?? request.Kind).Trim();
            var now = DateTimeOffset.UtcNow;
            var token = GenerateHmacToken(pi.Id, actingUser, decision, pi.TenantId);

            string oldStatus = pi.Status;
            string newStatus = oldStatus;

            if (decision.Equals("Approve", StringComparison.OrdinalIgnoreCase) || decision.Equals("Approved", StringComparison.OrdinalIgnoreCase))
            {
                if (pi.GrandTotal > 1000000m && pi.Status == ProformaInvoiceStatuses.Submitted)
                {
                    // Multi-tier escalation to Finance
                    newStatus = ProformaInvoiceStatuses.PendingFinanceApproval;
                }
                else
                {
                    newStatus = ProformaInvoiceStatuses.Approved;
                    pi.ApprovalToken = token;
                    pi.ApprovedBy = actingUser;
                    pi.ApprovedDate = now;
                }
            }
            else if (decision.Equals("Reject", StringComparison.OrdinalIgnoreCase) || decision.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            {
                newStatus = ProformaInvoiceStatuses.Rejected;
                pi.ApprovalToken = token;
            }
            else if (decision.Equals("Return", StringComparison.OrdinalIgnoreCase) || decision.Equals("Returned", StringComparison.OrdinalIgnoreCase))
            {
                newStatus = ProformaInvoiceStatuses.Returned;
                pi.ApprovalToken = token;
            }
            else if (decision.Equals("Submit", StringComparison.OrdinalIgnoreCase))
            {
                newStatus = ProformaInvoiceStatuses.Submitted;
            }
            else
            {
                throw new InvalidOperationException($"Unsupported decision '{decision}'.");
            }

            pi.Status = newStatus;
            pi.UpdatedBy = actingUser;
            pi.UpdatedDate = now;

            pi.ApprovalHistory.Add(new ProformaInvoiceApprovalHistory
            {
                EntryKey = Guid.NewGuid().ToString("N")[..8],
                ApprovalLevel = request.ApprovalLevel ?? (pi.Status == ProformaInvoiceStatuses.PendingFinanceApproval ? "Finance" : "Manager"),
                Decision = decision,
                Remarks = request.Remarks,
                ApprovedBy = actingUser,
                ApproverUserId = actingUser,
                ApprovalToken = token,
                IpAddress = ipAddress,
                ApprovedOn = now
            });

            pi.StatusHistory.Add(new ProformaInvoiceStatusHistory
            {
                EntryKey = Guid.NewGuid().ToString("N")[..8],
                OldStatus = oldStatus,
                NewStatus = newStatus,
                Remarks = $"Approval decision: {decision}. {request.Remarks}".Trim(),
                ChangedBy = actingUser,
                ChangedOn = now
            });

            await _db.SaveChangesAsync(ct);
            return ProformaInvoiceMapper.ToDto(pi);
        }
    }
}
