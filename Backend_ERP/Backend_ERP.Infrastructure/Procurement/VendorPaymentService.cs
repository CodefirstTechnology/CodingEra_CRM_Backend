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
    public class VendorPaymentService : IVendorPaymentService
    {
        private readonly ERPDbContext _dbContext;

        public VendorPaymentService(ERPDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<VendorPaymentDto> RecordPaymentAsync(CreateVendorPaymentDto dto, string currentUser, CancellationToken cancellationToken = default)
        {
            var allocatedTotal = dto.Allocations.Sum(x => x.AllocatedAmount);
            var unallocatedAmount = dto.TotalAmount - allocatedTotal;

            if (unallocatedAmount < 0)
            {
                throw new InvalidOperationException("Allocated amount cannot exceed total payment amount.");
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var paymentNumber = string.IsNullOrWhiteSpace(dto.PaymentNumber)
                    ? $"VP-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}"
                    : dto.PaymentNumber;

                var payment = new VendorPayment
                {
                    VendorId = dto.VendorId,
                    PaymentNumber = paymentNumber,
                    PaymentDate = dto.PaymentDate.ToUniversalTime(),
                    PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "NEFT/RTGS" : dto.PaymentMethod,
                    BankAccountId = dto.BankAccountId,
                    TotalAmount = dto.TotalAmount,
                    UnallocatedAmount = unallocatedAmount,
                    ReferenceNumber = dto.ReferenceNumber,
                    Status = "Cleared",
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = null
                };

                _dbContext.VendorPayments.Add(payment);
                await _dbContext.SaveChangesAsync(cancellationToken);

                var allocationResults = new List<PaymentAllocationResultDto>();

                foreach (var alloc in dto.Allocations)
                {
                    if (alloc.AllocatedAmount <= 0) continue;

                    var bill = await _dbContext.PurchaseBills
                        .FirstOrDefaultAsync(b => b.Id == alloc.PurchaseBillId && !b.IsDeleted, cancellationToken);

                    if (bill is null)
                    {
                        throw new InvalidOperationException($"Purchase Bill ID {alloc.PurchaseBillId} not found.");
                    }

                    var newAllocation = new VendorPaymentAllocation
                    {
                        VendorPaymentId = payment.Id,
                        PurchaseBillId = bill.Id,
                        AllocatedAmount = alloc.AllocatedAmount,
                        AllocatedAt = DateTime.UtcNow
                    };

                    _dbContext.VendorPaymentAllocations.Add(newAllocation);

                    bill.PaidAmount += alloc.AllocatedAmount;
                    bill.BalanceAmount = Math.Max(0m, bill.GrandTotal - bill.PaidAmount);

                    if (bill.PaidAmount >= bill.GrandTotal)
                    {
                        bill.PaymentStatus = PurchaseBillPaymentStatus.Paid;
                        bill.Status = PurchaseBillStatus.Paid;
                    }
                    else if (bill.PaidAmount > 0)
                    {
                        bill.PaymentStatus = PurchaseBillPaymentStatus.PartiallyPaid;
                    }

                    allocationResults.Add(new PaymentAllocationResultDto
                    {
                        VendorPaymentId = payment.Id,
                        PurchaseBillId = bill.Id,
                        BillNumber = bill.BillNumber,
                        AllocatedAmount = alloc.AllocatedAmount,
                        AllocatedAt = DateTime.UtcNow
                    });
                }

                // Write Accounts Payable Debit Entry into Vendor Ledger
                var ledgerEntry = new VendorLedgerEntry
                {
                    VendorId = dto.VendorId,
                    VoucherNumber = payment.PaymentNumber,
                    EntryDate = dto.PaymentDate.ToUniversalTime(),
                    EntryType = "Payment",
                    ReferenceId = payment.Id,
                    ReferenceNumber = dto.ReferenceNumber,
                    DebitAmount = dto.TotalAmount,
                    CreditAmount = 0m,
                    Narration = $"Payment Voucher {payment.PaymentNumber} recorded for Vendor ID {dto.VendorId}",
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.VendorLedgerEntries.Add(ledgerEntry);
                await _dbContext.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return new VendorPaymentDto
                {
                    Id = payment.Id,
                    VendorId = payment.VendorId,
                    PaymentNumber = payment.PaymentNumber,
                    PaymentDate = payment.PaymentDate,
                    PaymentMethod = payment.PaymentMethod,
                    BankAccountId = payment.BankAccountId,
                    TotalAmount = payment.TotalAmount,
                    UnallocatedAmount = payment.UnallocatedAmount,
                    ReferenceNumber = payment.ReferenceNumber,
                    Status = payment.Status,
                    CreatedAt = payment.CreatedAt,
                    Allocations = allocationResults
                };
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<List<VendorPaymentDto>> GetVendorPaymentsByVendorAsync(int vendorId, CancellationToken cancellationToken = default)
        {
            var payments = await _dbContext.VendorPayments
                .Include(p => p.Allocations)
                .ThenInclude(a => a.PurchaseBill)
                .Where(p => p.VendorId == vendorId)
                .OrderByDescending(p => p.PaymentDate)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return payments.Select(p => new VendorPaymentDto
            {
                Id = p.Id,
                VendorId = p.VendorId,
                PaymentNumber = p.PaymentNumber,
                PaymentDate = p.PaymentDate,
                PaymentMethod = p.PaymentMethod,
                BankAccountId = p.BankAccountId,
                TotalAmount = p.TotalAmount,
                UnallocatedAmount = p.UnallocatedAmount,
                ReferenceNumber = p.ReferenceNumber,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                Allocations = p.Allocations.Select(a => new PaymentAllocationResultDto
                {
                    Id = a.Id,
                    VendorPaymentId = a.VendorPaymentId,
                    PurchaseBillId = a.PurchaseBillId,
                    BillNumber = a.PurchaseBill != null ? a.PurchaseBill.BillNumber : string.Empty,
                    AllocatedAmount = a.AllocatedAmount,
                    AllocatedAt = a.AllocatedAt
                }).ToList()
            }).ToList();
        }
    }
}
