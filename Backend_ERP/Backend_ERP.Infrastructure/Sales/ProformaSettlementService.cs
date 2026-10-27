using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Sales
{
    public class ProformaSettlementService : IProformaSettlementService
    {
        private readonly ERPDbContext _db;
        private readonly IMtoProductionTriggerService _mtoService;

        public ProformaSettlementService(ERPDbContext db, IMtoProductionTriggerService mtoService)
        {
            _db = db;
            _mtoService = mtoService;
        }

        public async Task<ProformaSettlementResultDto> SettleReceiptAsync(
            int proformaInvoiceId,
            ProformaSettlementRequestDto request,
            string actingUser,
            CancellationToken ct = default)
        {
            if (request.AllocatedAmount <= 0)
            {
                throw new InvalidOperationException("Allocated settlement amount must be strictly greater than zero.");
            }

            var pi = await _db.ProformaInvoices
                .Include(x => x.SettlementAllocations)
                .FirstOrDefaultAsync(x => x.Id == proformaInvoiceId, ct);

            if (pi == null || pi.IsDeleted)
            {
                throw new InvalidOperationException($"Proforma Invoice #{proformaInvoiceId} not found or has been deleted.");
            }

            if (pi.Status is ProformaInvoiceStatuses.Cancelled or ProformaInvoiceStatuses.Rejected or ProformaInvoiceStatuses.Draft)
            {
                throw new InvalidOperationException($"Cannot settle payments against a proforma invoice in '{pi.Status}' state.");
            }

            decimal remainingUnsettled = Math.Max(0m, pi.GrandTotal - pi.AdvanceReceivedAmount);
            if (request.AllocatedAmount > (remainingUnsettled + 0.01m))
            {
                throw new InvalidOperationException(
                    $"Allocated amount (₹{request.AllocatedAmount:N2}) exceeds remaining unsettled balance (₹{remainingUnsettled:N2}) on PI '{pi.PiNumber}'.");
            }

            var advance = await _db.AdvancePayments
                .FirstOrDefaultAsync(x => x.Id == request.AdvancePaymentId, ct);

            if (advance == null || advance.IsDeleted)
            {
                throw new InvalidOperationException($"Advance Payment #{request.AdvancePaymentId} not found.");
            }

            if (advance.RemainingAmount < (request.AllocatedAmount - 0.01m))
            {
                throw new InvalidOperationException(
                    $"Advance Payment '{advance.PaymentNumber}' has insufficient unallocated balance. Available: ₹{advance.RemainingAmount:N2}, Requested: ₹{request.AllocatedAmount:N2}.");
            }

            var now = DateTimeOffset.UtcNow;
            var exchangeRate = request.ExchangeRate > 0 ? request.ExchangeRate : 1.0000m;
            var baseAmount = Math.Round(request.AllocatedAmount * exchangeRate, 2);

            // 1. Create Settlement Allocation Record
            var allocation = new ProformaInvoiceSettlementAllocation
            {
                TenantId = pi.TenantId,
                ProformaInvoiceId = pi.Id,
                AdvancePaymentId = advance.Id,
                AllocatedAmount = request.AllocatedAmount,
                ExchangeRateAtSettlement = exchangeRate,
                BaseCurrencyAllocatedAmount = baseAmount,
                ReceiptVoucherNumber = request.ReceiptVoucherNumber ?? advance.PaymentNumber,
                AllocatedOn = now,
                AllocatedBy = actingUser,
                Remarks = request.Remarks ?? $"Settlement against PI {pi.PiNumber}"
            };

            pi.SettlementAllocations.Add(allocation);

            // 2. Update Advance Payment Balances
            advance.AppliedAmount += request.AllocatedAmount;
            advance.RemainingAmount = Math.Max(0m, advance.AdvanceAmount - advance.AppliedAmount);
            advance.UpdatedBy = actingUser;
            advance.UpdatedDate = now;

            // 3. Update Proforma Invoice Balances & Status
            pi.AdvanceReceivedAmount += request.AllocatedAmount;
            if (pi.AdvanceReceivedAmount >= (pi.GrandTotal - 0.01m))
            {
                pi.PaymentStatus = "FullyPaid";
            }
            else
            {
                pi.PaymentStatus = "PartiallyPaid";
            }

            pi.UpdatedBy = actingUser;
            pi.UpdatedDate = now;

            // 4. Update Linked Sales Order Advance Balance (if linked)
            SalesOrder? linkedSo = null;
            if (pi.SalesOrderId.HasValue && pi.SalesOrderId.Value > 0)
            {
                linkedSo = await _db.SalesOrders
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x => x.Id == pi.SalesOrderId.Value, ct);

                if (linkedSo != null)
                {
                    linkedSo.AdvanceAllocatedAmount += request.AllocatedAmount;
                    linkedSo.UpdatedBy = actingUser;
                    linkedSo.UpdatedDate = now;
                }
            }

            // 5. Evaluate Two-Tier Operational Release Gates
            decimal advancePercentage = pi.GrandTotal > 0 ? (pi.AdvanceReceivedAmount / pi.GrandTotal) * 100m : 100m;

            // Tier 1 Gate: Production Release (>= 30% Token Advance)
            if (advancePercentage >= 30m && !pi.IsProductionReleased)
            {
                pi.IsProductionReleased = true;
                pi.ProductionReleasedOn = now;
                pi.ProductionReleasedBy = actingUser;

                if (linkedSo != null && !linkedSo.IsProductionReleased)
                {
                    linkedSo.IsProductionReleased = true;
                    linkedSo.ProductionReleasedDate = now;
                    linkedSo.ProductionReleasedBy = actingUser;

                    // Trigger MTO Work Order Priority Escalation
                    await _mtoService.EvaluateAndSpawnProductionAsync(linkedSo.Id, actingUser, ct);
                }
            }

            // Tier 2 Gate: Dispatch Release (100% Full Payment or Credit Term Override)
            if (pi.PaymentStatus == "FullyPaid" && !pi.IsDispatchReleased)
            {
                pi.IsDispatchReleased = true;
                pi.DispatchReleasedOn = now;
                pi.DispatchReleasedBy = actingUser;

                if (linkedSo != null && !linkedSo.IsDispatchReleased)
                {
                    linkedSo.IsDispatchReleased = true;
                    linkedSo.DispatchReleasedDate = now;
                    linkedSo.DispatchReleasedBy = actingUser;
                }
            }

            pi.StatusHistory.Add(new ProformaInvoiceStatusHistory
            {
                EntryKey = Guid.NewGuid().ToString("N")[..8],
                OldStatus = pi.Status,
                NewStatus = pi.Status,
                Remarks = $"Settlement of ₹{request.AllocatedAmount:N2} via Advance '{advance.PaymentNumber}'. Payment status: {pi.PaymentStatus}.",
                ChangedBy = actingUser,
                ChangedOn = now
            });

            await _db.SaveChangesAsync(ct);

            return new ProformaSettlementResultDto
            {
                ProformaInvoiceId = pi.Id,
                PiNumber = pi.PiNumber,
                TotalAmount = pi.GrandTotal,
                AdvanceReceivedAmount = pi.AdvanceReceivedAmount,
                RemainingUnsettledAmount = Math.Max(0m, pi.GrandTotal - pi.AdvanceReceivedAmount),
                PaymentStatus = pi.PaymentStatus,
                IsProductionReleased = pi.IsProductionReleased,
                IsDispatchReleased = pi.IsDispatchReleased,
                Message = $"Settlement successfully applied. Production Released: {pi.IsProductionReleased}, Dispatch Released: {pi.IsDispatchReleased}."
            };
        }

        public async Task<bool> ReverseSettlementAsync(
            int settlementAllocationId,
            string reason,
            string actingUser,
            CancellationToken ct = default)
        {
            var allocation = await _db.ProformaInvoiceSettlementAllocations
                .Include(x => x.ProformaInvoice)
                .Include(x => x.AdvancePayment)
                .FirstOrDefaultAsync(x => x.Id == settlementAllocationId, ct);

            if (allocation == null || allocation.IsReversed)
            {
                return false;
            }

            var now = DateTimeOffset.UtcNow;
            allocation.IsReversed = true;
            allocation.ReversedOn = now;
            allocation.Remarks += $" [Reversed: {reason}]";

            // Restore AdvancePayment balance
            allocation.AdvancePayment.AppliedAmount = Math.Max(0m, allocation.AdvancePayment.AppliedAmount - allocation.AllocatedAmount);
            allocation.AdvancePayment.RemainingAmount = Math.Max(0m, allocation.AdvancePayment.AdvanceAmount - allocation.AdvancePayment.AppliedAmount);

            // Restore ProformaInvoice balance
            var pi = allocation.ProformaInvoice;
            pi.AdvanceReceivedAmount = Math.Max(0m, pi.AdvanceReceivedAmount - allocation.AllocatedAmount);
            pi.PaymentStatus = pi.AdvanceReceivedAmount > 0 ? "PartiallyPaid" : "Unpaid";

            // If linked to SO, restore SO balance
            if (pi.SalesOrderId.HasValue && pi.SalesOrderId.Value > 0)
            {
                var so = await _db.SalesOrders.FirstOrDefaultAsync(x => x.Id == pi.SalesOrderId.Value, ct);
                if (so != null)
                {
                    so.AdvanceAllocatedAmount = Math.Max(0m, so.AdvanceAllocatedAmount - allocation.AllocatedAmount);
                }
            }

            await _db.SaveChangesAsync(ct);
            return true;
        }
    }
}
