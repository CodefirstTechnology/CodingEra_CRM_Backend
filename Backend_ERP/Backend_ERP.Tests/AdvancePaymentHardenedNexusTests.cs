using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Backend_ERP.Tests
{
    /// <summary>
    /// Automated verification suite for Submodule 5 â€” Advance Payments Hub.
    /// Validates all five critical requirements from the execution specification:
    ///   1. Over-allocation beyond unbilled SO balance throws ceiling violation.
    ///   2. Advance allocation >= 30% triggers IsProductionReleased = true and calls IMtoProductionTriggerService.
    ///   3. Advance allocation = 100% triggers IsDispatchReleased = true.
    ///   4. Cheque dishonour rolls back allocations and freezes operational gates.
    ///   5. Multi-currency Tax Invoice adjustment calculates Realized FX Gain/Loss accurately.
    /// </summary>
    public class AdvancePaymentHardenedNexusTests
    {
        // â”€â”€â”€ Infrastructure helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private ERPDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ERPDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ERPDbContext(options);
        }

        private (AdvancePaymentService Service, ERPDbContext Db) CreateApplyService(ERPDbContext db)
        {
            var repo = new AdvancePaymentRepository(db);
            var numbering = new AdvancePaymentNumberingService(db);
            var service = new AdvancePaymentService(repo, numbering, db);
            return (service, db);
        }

        private (AdvancePaymentSettlementService SettlementService, ERPDbContext Db) CreateSettlementService(ERPDbContext db)
        {
            var numbering = new AdvancePaymentNumberingService(db);
            var mtoTrigger = new MockMtoProductionTriggerService();
            var logger = NullLogger<AdvancePaymentSettlementService>.Instance;
            var service = new AdvancePaymentSettlementService(db, numbering, mtoTrigger, logger);
            return (service, db);
        }

        /// <summary>
        /// Spy/mock for IMtoProductionTriggerService that captures calls.
        /// </summary>
        private class MockMtoProductionTriggerService : IMtoProductionTriggerService
        {
            public List<int> CalledForSalesOrderIds { get; } = new();
            public bool WasCalled => CalledForSalesOrderIds.Count > 0;

            public Task<MtoEvaluationResultDto> EvaluateAndSpawnProductionAsync(
                int salesOrderId,
                string actingUser,
                System.Threading.CancellationToken ct = default)
            {
                CalledForSalesOrderIds.Add(salesOrderId);
                return Task.FromResult(new MtoEvaluationResultDto());
            }
        }

        // â”€â”€â”€ TEST 1: Over-allocation beyond unbilled SO balance â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Fact]
        public async Task Test1_OverAllocation_BeyondUnbilledSOBalance_ThrowsCeilingViolation()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var (service, _) = CreateApplyService(db);

            var so = new SalesOrder
            {
                Id = 1001,
                SalesOrderNumber = "SO-HAR-1001",
                CustomerName = "Ceilings Ltd",
                OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Subtotal = 50_000m,
                GrandTotal = 50_000m,
                AdvanceAllocatedAmount = 0m,
                Status = SalesOrderStatuses.Confirmed
            };
            db.SalesOrders.Add(so);

            var adv = new AdvancePayment
            {
                Id = 2001,
                PaymentNumber = "ADV-HAR-2001",
                CustomerId = "C1",
                CustomerName = "Ceilings Ltd",
                PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
                PaymentMode = AdvancePaymentModes.NEFT,
                Currency = "INR",
                ExchangeRate = 1m,
                AdvanceAmount = 100_000m,
                AppliedAmount = 0m,
                RemainingAmount = 100_000m,
                Status = AdvancePaymentStatuses.Received
            };
            db.AdvancePayments.Add(adv);
            await db.SaveChangesAsync();

            // Act: Attempt to apply 60,000 against a SO with GrandTotal only 50,000
            var overRequest = new AdvancePaymentApplyRequestDto
            {
                SalesOrderId = 1001,
                SalesOrderNumber = "SO-HAR-1001",
                ApplyAmount = 60_000m,
                Remarks = "Over-allocation attempt"
            };

            // Assert: Must throw ceiling violation
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ApplyAsync(2001, overRequest, "TestUser"));

            Assert.Contains("exceeds remaining payable balance", ex.Message, StringComparison.OrdinalIgnoreCase);

            // Balances must be unchanged
            var unchanged = await db.AdvancePayments.FindAsync(2001);
            Assert.NotNull(unchanged);
            Assert.Equal(0m, unchanged!.AppliedAmount);
            Assert.Equal(100_000m, unchanged.RemainingAmount);

            var unchangedSo = await db.SalesOrders.FindAsync(1001);
            Assert.NotNull(unchangedSo);
            Assert.Equal(0m, unchangedSo!.AdvanceAllocatedAmount);
        }

        // â”€â”€â”€ TEST 2: Allocation >= 30% triggers IsProductionReleased â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Fact]
        public async Task Test2_Allocation30Pct_TriggersProductionReleasedAndCallsMtoService()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var (settlementSvc, _) = CreateSettlementService(db);
            var mtoSpy = (MockMtoProductionTriggerService)settlementSvc
                .GetType()
                .GetField("_mtoTrigger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .GetValue(settlementSvc)!;

            var so = new SalesOrder
            {
                Id = 1002,
                SalesOrderNumber = "SO-HAR-1002",
                CustomerName = "Precision Eng",
                OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Subtotal = 100_000m,
                GrandTotal = 100_000m,
                AdvanceAllocatedAmount = 30_000m,   // 30% already allocated via trigger
                IsProductionReleased = false,
                IsDispatchReleased = false,
                Status = SalesOrderStatuses.Confirmed
            };
            db.SalesOrders.Add(so);
            await db.SaveChangesAsync();

            // Act: SynchronizeOperationalGatesAsync is called after BRS clear
            await settlementSvc.SynchronizeOperationalGatesAsync(1002);

            // Assert Tier 1 production gate fired
            var updated = await db.SalesOrders.FindAsync(1002);
            Assert.NotNull(updated);
            Assert.True(updated!.IsProductionReleased,
                "IsProductionReleased must be TRUE when allocated >= 30%.");

            // Assert IMtoProductionTriggerService.EvaluateAndSpawnProductionAsync was called
            Assert.True(mtoSpy.WasCalled,
                "IMtoProductionTriggerService.EvaluateAndSpawnProductionAsync must be called.");
            Assert.Contains(1002, mtoSpy.CalledForSalesOrderIds);

            // Dispatch not yet released (< 100%)
            Assert.False(updated.IsDispatchReleased,
                "IsDispatchReleased must remain FALSE at 30% allocation.");
        }

        // â”€â”€â”€ TEST 3: Allocation = 100% triggers IsDispatchReleased â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        [Fact]
        public async Task Test3_Allocation100Pct_TriggersDispatchReleased()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var (settlementSvc, _) = CreateSettlementService(db);

            var so = new SalesOrder
            {
                Id = 1003,
                SalesOrderNumber = "SO-HAR-1003",
                CustomerName = "Full Payment Corp",
                OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Subtotal = 75_000m,
                GrandTotal = 75_000m,
                AdvanceAllocatedAmount = 75_000m,   // 100% allocated via trigger
                IsProductionReleased = true,
                IsDispatchReleased = false,
                Status = SalesOrderStatuses.Confirmed
            };
            db.SalesOrders.Add(so);
            await db.SaveChangesAsync();

            // Act
            await settlementSvc.SynchronizeOperationalGatesAsync(1003);

            // Assert Tier 2 dispatch gate fired
            var updated = await db.SalesOrders.FindAsync(1003);
            Assert.NotNull(updated);
            Assert.True(updated!.IsProductionReleased, "IsProductionReleased must remain TRUE at 100%.");
            Assert.True(updated.IsDispatchReleased,
                "IsDispatchReleased must be TRUE when allocated = 100%.");
        }

        // â”€â”€â”€ TEST 4: Cheque dishonour rolls back allocations and freezes gates â”€â”€â”€â”€

        [Fact]
        public async Task Test4_ChequeDishonour_RollsBackAllocations_FreezesOperationalGates()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var (settlementSvc, _) = CreateSettlementService(db);

            var so = new SalesOrder
            {
                Id = 1004,
                SalesOrderNumber = "SO-HAR-1004",
                CustomerName = "Bounce Corp",
                OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Subtotal = 100_000m,
                GrandTotal = 100_000m,
                AdvanceAllocatedAmount = 50_000m,
                IsProductionReleased = true,
                IsDispatchReleased = false,
                Status = SalesOrderStatuses.Confirmed
            };
            db.SalesOrders.Add(so);

            var adv = new AdvancePayment
            {
                Id = 2004,
                PaymentNumber = "ADV-HAR-2004",
                CustomerId = "C4",
                CustomerName = "Bounce Corp",
                PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
                PaymentMode = AdvancePaymentModes.Cheque,
                Currency = "INR",
                ExchangeRate = 1m,
                AdvanceAmount = 50_000m,
                AppliedAmount = 50_000m,
                RemainingAmount = 0m,
                TenantId = "default",
                Status = AdvancePaymentStatuses.Received
            };
            db.AdvancePayments.Add(adv);

            // Active application linking advance to SO
            var app = new AdvancePaymentApplication
            {
                Id = 3001,
                TenantId = "default",
                AdvancePaymentId = 2004,
                SalesOrderId = 1004,
                SalesOrderNumber = "SO-HAR-1004",
                ApplyAmount = 50_000m,
                ExchangeRateAtAllocation = 1m,
                RealizedFxGainLoss = 0m,
                IsReversal = false,
                Remarks = "Initial allocation",
                AppliedBy = "TestUser",
                AppliedOn = DateTimeOffset.UtcNow.AddDays(-5)
            };
            db.AdvancePaymentApplications.Add(app);
            await db.SaveChangesAsync();

            // Act: Dishonour the cheque
            var dishonourReq = new AdvancePaymentDishonorRequestDto
            {
                DishonorReason = "Cheque returned â€“ insufficient funds (MICR mismatch)",
                DishonorDate = DateTime.UtcNow.ToString("yyyy-MM-dd")
            };

            var result = await settlementSvc.DishonorPaymentAsync(2004, dishonourReq, "FinanceManager");

            // Assert: Advance payment marked Dishonored
            Assert.NotNull(result);
            Assert.Equal(AdvancePaymentStatuses.Dishonored, result.Status);

            // Assert: Compensating reversals created
            var reversals = await db.AdvancePaymentApplications
                .Where(a => a.AdvancePaymentId == 2004 && a.IsReversal)
                .ToListAsync();
            Assert.NotEmpty(reversals);

            // Assert: SO.AdvanceAllocatedAmount decremented
            var updatedSo = await db.SalesOrders.FindAsync(1004);
            Assert.NotNull(updatedSo);
            Assert.Equal(0m, updatedSo!.AdvanceAllocatedAmount);

            // Assert: Tier 2 Dispatch Gate tripped
            Assert.False(updatedSo.IsDispatchReleased,
                "IsDispatchReleased must be FROZEN (false) after dishonour.");

            // Assert: Tier 1 Production Gate tripped (< 30% after rollback)
            Assert.False(updatedSo.IsProductionReleased,
                "IsProductionReleased must be FROZEN (false) after dishonour when allocation drops below 30%.");

            // Assert: Credit hold reason recorded
            Assert.NotNull(updatedSo.CreditHoldReason);
            Assert.Contains("dishonored", updatedSo.CreditHoldReason!, StringComparison.OrdinalIgnoreCase);
        }

        // â”€â”€â”€ TEST 5: Multi-currency Tax Invoice adjustment â€” Realized FX Gain/Loss â”€

        [Fact]
        public async Task Test5_MultiCurrency_TaxInvoiceAdjustment_CalculatesRealizedFxGainLossAccurately()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var (settlementSvc, _) = CreateSettlementService(db);

            // USD 500 booked at â‚¹83.00/USD
            var adv = new AdvancePayment
            {
                Id = 2005,
                PaymentNumber = "ADV-HAR-2005",
                CustomerId = "C5",
                CustomerName = "Global Exports Ltd",
                PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-20)),
                PaymentMode = AdvancePaymentModes.BankTransfer,
                Currency = "USD",
                ExchangeRate = 83.00m,
                AdvanceAmount = 500m,
                AppliedAmount = 0m,
                RemainingAmount = 500m,
                TenantId = "default",
                Status = AdvancePaymentStatuses.Received
            };
            db.AdvancePayments.Add(adv);

            // Receipt Voucher for GSTR-1 Table 11 offset
            var voucher = new AdvancePaymentReceiptVoucher
            {
                Id = 4001,
                TenantId = "default",
                VoucherNumber = "RV-HAR-4001",
                AdvancePaymentId = 2005,
                CustomerId = 5,
                CustomerName = "Global Exports Ltd",
                PlaceOfSupply = "Gujarat",
                PlaceOfSupplyStateCode = "24",
                IsInterState = true,
                TaxableAmount = 423.73m,
                CgstRate = 0m,
                CgstAmount = 0m,
                SgstRate = 0m,
                SgstAmount = 0m,
                IgstRate = 18.00m,
                IgstAmount = 76.27m,
                TotalVoucherAmount = 500m,
                IsAdjusted = false,
                VoucherDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-20)),
                CreatedDate = DateTimeOffset.UtcNow.AddDays(-20),
                CreatedBy = "FinanceUser"
            };
            db.AdvancePaymentReceiptVouchers.Add(voucher);
            await db.SaveChangesAsync();

            // Act: Adjust USD 500 against Tax Invoice at current spot rate â‚¹84.50/USD
            // Expected Realized FX Gain = 500 * (84.50 - 83.00) = +750.00 (Gain)
            var adjustReq = new AdvancePaymentAdjustmentRequestDto
            {
                AdvancePaymentId = 2005,
                SalesInvoiceId = null,
                InvoiceNumber = "INV-EXPORT-2026-001",
                AdjustedAmount = 500m,
                AdjustmentDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ExchangeRateAtAdjustment = 84.50m,
                Remarks = "Full adjustment at invoice date spot rate"
            };

            var result = await settlementSvc.AdjustAgainstTaxInvoiceAsync(adjustReq, "TreasuryManager");

            // Assert: Adjustment record created with correct FX calculation
            Assert.NotNull(result);
            Assert.Equal("INV-EXPORT-2026-001", result.InvoiceNumber);
            Assert.Equal(500m, result.AdjustedAmount);
            Assert.Equal(84.50m, result.ExchangeRateAtAdjustment);

            // RealizedFxGainLoss = Round(500 * (84.50 - 83.00), 2) = Round(750.00, 2) = 750.00
            Assert.Equal(750.00m, result.RealizedFxGainLoss);

            // Assert: GSTR-1 Table 11B offset â€” Gstr1VoucherRef populated from unadjusted voucher
            Assert.Equal("RV-HAR-4001", result.Gstr1VoucherRef);

            // Assert: Receipt voucher marked as adjusted (no duplicate tax booking)
            var adjustedVoucher = await db.AdvancePaymentReceiptVouchers.FindAsync(4001);
            Assert.NotNull(adjustedVoucher);
            Assert.True(adjustedVoucher!.IsAdjusted,
                "Receipt voucher must be marked IsAdjusted = true to prevent duplicate GSTR-1 tax booking.");

            // Assert: Advance payment balance fully depleted
            var updatedAdv = await db.AdvancePayments.FindAsync(2005);
            Assert.NotNull(updatedAdv);
            Assert.Equal(500m, updatedAdv!.AppliedAmount);
            Assert.Equal(0m, updatedAdv.RemainingAmount);
            Assert.Equal(AdvancePaymentStatuses.FullyApplied, updatedAdv.Status);

            // Assert: DB persisted record matches
            var persistedAdj = await db.AdvancePaymentAdjustments
                .FirstOrDefaultAsync(a => a.AdvancePaymentId == 2005);
            Assert.NotNull(persistedAdj);
            Assert.Equal(750.00m, persistedAdj!.RealizedFxGainLoss);
        }
    }
}

