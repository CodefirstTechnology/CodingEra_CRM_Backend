using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Sales;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend_ERP.Tests;

public class AdvancePaymentPhase1Tests
{
    private ERPDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ERPDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ERPDbContext(options);
    }

    private (AdvancePaymentService Service, ERPDbContext Db) CreateService(ERPDbContext db)
    {
        var repo = new AdvancePaymentRepository(db);
        var numbering = new AdvancePaymentNumberingService(db);
        var service = new AdvancePaymentService(repo, numbering, db);
        return (service, db);
    }

    // ─── TEST 1: Order-Side Payable Balance Cap Enforced ──────────────────────
    [Fact]
    public async Task Test_OrderSide_PayableBalance_Cap_Enforced()
    {
        using var db = CreateInMemoryDbContext();
        var (service, _) = CreateService(db);

        var so = new SalesOrder
        {
            Id = 101,
            SalesOrderNumber = "SO-2026-0101",
            CustomerName = "Acme Corp",
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Subtotal = 50000m,
            GrandTotal = 50000m,
            AdvanceAllocatedAmount = 0m,
            Status = SalesOrderStatuses.Confirmed
        };
        db.SalesOrders.Add(so);

        var adv = new AdvancePayment
        {
            Id = 201,
            PaymentNumber = "ADV-2026-0201",
            CustomerId = "1",
            CustomerName = "Acme Corp",
            PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PaymentMode = AdvancePaymentModes.NEFT,
            Currency = "INR",
            AdvanceAmount = 100000m,
            AppliedAmount = 0m,
            RemainingAmount = 100000m,
            Status = AdvancePaymentStatuses.Received
        };
        db.AdvancePayments.Add(adv);
        await db.SaveChangesAsync();

        // 1. Attempt to apply 60000 against an order with GrandTotal 50000
        var overRequest = new AdvancePaymentApplyRequestDto
        {
            SalesOrderId = 101,
            SalesOrderNumber = "SO-2026-0101",
            ApplyAmount = 60000m
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ApplyAsync(201, overRequest, "TestUser"));
        Assert.Contains("exceeds remaining payable balance", ex.Message, StringComparison.OrdinalIgnoreCase);

        // 2. Apply valid amount of 40000
        var validRequest = new AdvancePaymentApplyRequestDto
        {
            SalesOrderId = 101,
            SalesOrderNumber = "SO-2026-0101",
            ApplyAmount = 40000m
        };
        var result = await service.ApplyAsync(201, validRequest, "TestUser");

        Assert.Equal(40000m, result.AppliedAmount);
        Assert.Equal(60000m, result.RemainingAmount);
        Assert.Equal(AdvancePaymentStatuses.PartiallyApplied, result.Status);

        var updatedSo = await db.SalesOrders.FindAsync(101);
        Assert.NotNull(updatedSo);
        Assert.Equal(40000m, updatedSo.AdvanceAllocatedAmount);

        // 3. Attempt to apply another 20000 (order remaining payable balance is only 10000)
        var secondOverRequest = new AdvancePaymentApplyRequestDto
        {
            SalesOrderId = 101,
            SalesOrderNumber = "SO-2026-0101",
            ApplyAmount = 20000m
        };
        var ex2 = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ApplyAsync(201, secondOverRequest, "TestUser"));
        Assert.Contains("exceeds remaining payable balance", ex2.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ─── TEST 2: Partial Reversal Workflow ────────────────────────────────────
    [Fact]
    public async Task Test_Partial_Reversal_Workflow()
    {
        using var db = CreateInMemoryDbContext();
        var (service, _) = CreateService(db);

        var so = new SalesOrder
        {
            Id = 102,
            SalesOrderNumber = "SO-2026-0102",
            CustomerName = "Acme Corp",
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
            GrandTotal = 50000m,
            AdvanceAllocatedAmount = 0m,
            Status = SalesOrderStatuses.Confirmed
        };
        db.SalesOrders.Add(so);

        var adv = new AdvancePayment
        {
            Id = 202,
            PaymentNumber = "ADV-2026-0202",
            CustomerId = "1",
            CustomerName = "Acme Corp",
            PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PaymentMode = AdvancePaymentModes.NEFT,
            Currency = "INR",
            AdvanceAmount = 50000m,
            AppliedAmount = 0m,
            RemainingAmount = 50000m,
            Status = AdvancePaymentStatuses.Received
        };
        db.AdvancePayments.Add(adv);
        await db.SaveChangesAsync();

        // Apply 30000
        var applyReq = new AdvancePaymentApplyRequestDto
        {
            SalesOrderId = 102,
            SalesOrderNumber = "SO-2026-0102",
            ApplyAmount = 30000m
        };
        await service.ApplyAsync(202, applyReq, "TestUser");

        var originalApp = await db.AdvancePaymentApplications.FirstAsync(x => x.AdvancePaymentId == 202);
        Assert.False(originalApp.IsReversal);
        Assert.Equal(30000m, originalApp.ApplyAmount);

        // Reverse partial amount of 10000
        var reverseReq = new ReverseAllocationRequestDto
        {
            ApplicationId = originalApp.Id,
            ReversalAmount = 10000m,
            Reason = "Order quantity reduced by customer"
        };
        var updatedAdv = await service.ReverseApplicationAsync(202, reverseReq, "TestUser");

        // Assert advance payment balances
        Assert.Equal(20000m, updatedAdv.AppliedAmount);
        Assert.Equal(30000m, updatedAdv.RemainingAmount);
        Assert.Equal(AdvancePaymentStatuses.PartiallyApplied, updatedAdv.Status);

        // Assert compensating application record
        var reversalApp = await db.AdvancePaymentApplications
            .FirstOrDefaultAsync(x => x.AdvancePaymentId == 202 && x.IsReversal);
        Assert.NotNull(reversalApp);
        Assert.True(reversalApp.IsReversal);
        Assert.Equal(-10000m, reversalApp.ApplyAmount);
        Assert.Equal(originalApp.Id, reversalApp.OriginalApplicationId);
        Assert.Equal("Order quantity reduced by customer", reversalApp.ReversalReason);

        // Assert sales order balance restored
        var updatedSo = await db.SalesOrders.FindAsync(102);
        Assert.NotNull(updatedSo);
        Assert.Equal(20000m, updatedSo.AdvanceAllocatedAmount);

        // Assert timeline has Reversed entry
        var timeline = await db.AdvancePaymentTimelines
            .Where(x => x.AdvancePaymentId == 202 && x.Action == AdvancePaymentTimelineActions.Reversed)
            .ToListAsync();
        Assert.NotEmpty(timeline);
    }

    // ─── TEST 3: Full Reversal Restores Received Status ───────────────────────
    [Fact]
    public async Task Test_Full_Reversal_Restores_Received_Status()
    {
        using var db = CreateInMemoryDbContext();
        var (service, _) = CreateService(db);

        var so = new SalesOrder
        {
            Id = 103,
            SalesOrderNumber = "SO-2026-0103",
            CustomerName = "Global Logistics",
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
            GrandTotal = 25000m,
            AdvanceAllocatedAmount = 0m,
            Status = SalesOrderStatuses.Confirmed
        };
        db.SalesOrders.Add(so);

        var adv = new AdvancePayment
        {
            Id = 203,
            PaymentNumber = "ADV-2026-0203",
            CustomerId = "2",
            CustomerName = "Global Logistics",
            PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PaymentMode = AdvancePaymentModes.BankTransfer,
            Currency = "INR",
            AdvanceAmount = 25000m,
            AppliedAmount = 0m,
            RemainingAmount = 25000m,
            Status = AdvancePaymentStatuses.Received
        };
        db.AdvancePayments.Add(adv);
        await db.SaveChangesAsync();

        // 1. Fully apply advance
        var applyReq = new AdvancePaymentApplyRequestDto
        {
            SalesOrderId = 103,
            SalesOrderNumber = "SO-2026-0103",
            ApplyAmount = 25000m
        };
        var appliedDto = await service.ApplyAsync(203, applyReq, "TestUser");
        Assert.Equal(AdvancePaymentStatuses.FullyApplied, appliedDto.Status);
        Assert.Equal(0m, appliedDto.RemainingAmount);

        var originalApp = await db.AdvancePaymentApplications.FirstAsync(x => x.AdvancePaymentId == 203);

        // 2. Attempt to reverse more than the applied amount (e.g. 30000)
        var invalidRev = new ReverseAllocationRequestDto
        {
            ApplicationId = originalApp.Id,
            ReversalAmount = 30000m,
            Reason = "Testing over-reversal"
        };
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReverseApplicationAsync(203, invalidRev, "TestUser"));
        Assert.Contains("exceeds available unreversed amount", ex.Message);

        // 3. Reverse entire 25000
        var fullRev = new ReverseAllocationRequestDto
        {
            ApplicationId = originalApp.Id,
            ReversalAmount = 25000m,
            Reason = "Order cancelled completely"
        };
        var fullyReversedDto = await service.ReverseApplicationAsync(203, fullRev, "TestUser");

        Assert.Equal(AdvancePaymentStatuses.Received, fullyReversedDto.Status);
        Assert.Equal(0m, fullyReversedDto.AppliedAmount);
        Assert.Equal(25000m, fullyReversedDto.RemainingAmount);

        var updatedSo = await db.SalesOrders.FindAsync(103);
        Assert.NotNull(updatedSo);
        Assert.Equal(0m, updatedSo.AdvanceAllocatedAmount);

        // 4. Attempting another reversal on the already fully reversed application is rejected
        var duplicateRev = new ReverseAllocationRequestDto
        {
            ApplicationId = originalApp.Id,
            ReversalAmount = 5000m,
            Reason = "Attempt duplicate reversal"
        };
        var ex2 = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReverseApplicationAsync(203, duplicateRev, "TestUser"));
        Assert.Contains("exceeds available unreversed amount", ex2.Message);
    }

    // ─── TEST 4: Mathematical Invariant Across Lifecycle ──────────────────────
    [Fact]
    public async Task Test_Mathematical_Invariant_Across_Lifecycle()
    {
        using var db = CreateInMemoryDbContext();
        var (service, _) = CreateService(db);

        var so = new SalesOrder
        {
            Id = 104,
            SalesOrderNumber = "SO-2026-0104",
            CustomerName = "Metro Traders",
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
            GrandTotal = 60000m,
            AdvanceAllocatedAmount = 0m,
            Status = SalesOrderStatuses.Confirmed
        };
        db.SalesOrders.Add(so);

        var adv = new AdvancePayment
        {
            Id = 204,
            PaymentNumber = "ADV-2026-0204",
            CustomerId = "3",
            CustomerName = "Metro Traders",
            PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PaymentMode = AdvancePaymentModes.NEFT,
            Currency = "INR",
            AdvanceAmount = 100000m,
            AppliedAmount = 0m,
            RemainingAmount = 100000m,
            RefundedAmount = 0m,
            ForfeitedAmount = 0m,
            Status = AdvancePaymentStatuses.Received
        };
        db.AdvancePayments.Add(adv);
        await db.SaveChangesAsync();

        // 1. Apply 40000
        await service.ApplyAsync(204, new AdvancePaymentApplyRequestDto
        {
            SalesOrderId = 104,
            SalesOrderNumber = "SO-2026-0104",
            ApplyAmount = 40000m
        }, "TestUser");

        var app = await db.AdvancePaymentApplications.FirstAsync(x => x.AdvancePaymentId == 204);

        // 2. Reverse 10000
        await service.ReverseApplicationAsync(204, new ReverseAllocationRequestDto
        {
            ApplicationId = app.Id,
            ReversalAmount = 10000m,
            Reason = "Reversing 10k"
        }, "TestUser");

        var check1 = await db.AdvancePayments.FindAsync(204);
        Assert.NotNull(check1);
        Assert.Equal(100000m, check1.AdvanceAmount);
        Assert.Equal(30000m, check1.AppliedAmount);
        Assert.Equal(70000m, check1.RemainingAmount);
        Assert.Equal(check1.AdvanceAmount - check1.AppliedAmount - check1.RefundedAmount - check1.ForfeitedAmount, check1.RemainingAmount);

        // 3. Process Refund of 50000
        var refundDto = await service.ProcessRefundAsync(204, new ProcessRefundRequestDto
        {
            RefundAmount = 50000m,
            RefundReferenceNumber = "UTR-999888777",
            Remarks = "Customer requested partial refund"
        }, "FinanceUser");

        Assert.Equal(50000m, refundDto.RefundedAmount);
        Assert.Equal(20000m, refundDto.RemainingAmount);
        Assert.Equal(AdvancePaymentStatuses.PartiallyApplied, refundDto.Status);

        var check2 = await db.AdvancePayments.FindAsync(204);
        Assert.NotNull(check2);
        Assert.Equal(check2.AdvanceAmount - check2.AppliedAmount - check2.RefundedAmount - check2.ForfeitedAmount, check2.RemainingAmount);

        // 4. Process Forfeiture of remaining 20000
        var forfeitDto = await service.ProcessForfeitureAsync(204, new ProcessForfeitureRequestDto
        {
            ForfeitureAmount = 20000m,
            Reason = "Customer defaulted on terms; liquidated damages"
        }, "LegalUser");

        Assert.Equal(20000m, forfeitDto.ForfeitedAmount);
        Assert.Equal(0m, forfeitDto.RemainingAmount);
        // Fully applied / settled
        Assert.Equal(AdvancePaymentStatuses.FullyApplied, forfeitDto.Status);

        var finalAdv = await db.AdvancePayments.FindAsync(204);
        Assert.NotNull(finalAdv);
        Assert.Equal(100000m, finalAdv.AdvanceAmount);
        Assert.Equal(30000m, finalAdv.AppliedAmount);
        Assert.Equal(50000m, finalAdv.RefundedAmount);
        Assert.Equal(20000m, finalAdv.ForfeitedAmount);
        Assert.Equal(0m, finalAdv.RemainingAmount);

        // Invariant holds strictly: Remaining == Advance - Applied - Refunded - Forfeited
        Assert.Equal(finalAdv.AdvanceAmount - finalAdv.AppliedAmount - finalAdv.RefundedAmount - finalAdv.ForfeitedAmount, finalAdv.RemainingAmount);
    }

    // ─── TEST 5: Concurrency and Balance Depletion Guard ─────────────────────
    [Fact]
    public async Task Test_Concurrency_And_BalanceDepletion_Guard()
    {
        using var db = CreateInMemoryDbContext();
        var (service, _) = CreateService(db);

        var so1 = new SalesOrder
        {
            Id = 105,
            SalesOrderNumber = "SO-2026-0105",
            CustomerName = "Alpha Systems",
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
            GrandTotal = 15000m,
            AdvanceAllocatedAmount = 0m,
            Status = SalesOrderStatuses.Confirmed
        };
        var so2 = new SalesOrder
        {
            Id = 106,
            SalesOrderNumber = "SO-2026-0106",
            CustomerName = "Alpha Systems",
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
            GrandTotal = 15000m,
            AdvanceAllocatedAmount = 0m,
            Status = SalesOrderStatuses.Confirmed
        };
        db.SalesOrders.AddRange(so1, so2);

        var adv = new AdvancePayment
        {
            Id = 205,
            PaymentNumber = "ADV-2026-0205",
            CustomerId = "4",
            CustomerName = "Alpha Systems",
            PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PaymentMode = AdvancePaymentModes.NEFT,
            Currency = "INR",
            AdvanceAmount = 20000m,
            AppliedAmount = 0m,
            RemainingAmount = 20000m,
            Status = AdvancePaymentStatuses.Received
        };
        db.AdvancePayments.Add(adv);
        await db.SaveChangesAsync();

        // Allocation 1: takes 15000
        var req1 = new AdvancePaymentApplyRequestDto
        {
            SalesOrderId = 105,
            SalesOrderNumber = "SO-2026-0105",
            ApplyAmount = 15000m
        };
        var res1 = await service.ApplyAsync(205, req1, "User1");
        Assert.Equal(5000m, res1.RemainingAmount);

        // Allocation 2: tries to take 15000 when only 5000 remains -> MUST FAIL
        var req2 = new AdvancePaymentApplyRequestDto
        {
            SalesOrderId = 106,
            SalesOrderNumber = "SO-2026-0106",
            ApplyAmount = 15000m
        };
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ApplyAsync(205, req2, "User2"));
        Assert.Contains("exceeds remaining amount", ex.Message);

        // Allocation 2 retry: takes exact remaining 5000 -> succeeds
        var req2Valid = new AdvancePaymentApplyRequestDto
        {
            SalesOrderId = 106,
            SalesOrderNumber = "SO-2026-0106",
            ApplyAmount = 5000m
        };
        var res2 = await service.ApplyAsync(205, req2Valid, "User2");
        Assert.Equal(0m, res2.RemainingAmount);
        Assert.Equal(20000m, res2.AppliedAmount);
        Assert.Equal(AdvancePaymentStatuses.FullyApplied, res2.Status);

        // Attempting to apply anything on a FullyApplied payment must fail
        var req3 = new AdvancePaymentApplyRequestDto
        {
            SalesOrderId = 106,
            SalesOrderNumber = "SO-2026-0106",
            ApplyAmount = 100m
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ApplyAsync(205, req3, "User3"));
    }
}
