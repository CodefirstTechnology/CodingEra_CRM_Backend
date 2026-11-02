using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace ERP.Tests
{
    public class GoodsReceiptAndPurchaseBillsEnterpriseTests
    {
        private ERPDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ERPDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new ERPDbContext(options);
        }

        [Fact]
        public void MovingWac_CalculatesAccurateWeightedAverage()
        {
            using var db = CreateDbContext();
            var valuationEngine = new InventoryValuationEngine(db);

            // Existing Stock: 100 units @ ₹50.00 each = ₹5,000
            // Inward QC Accepted: 50 units @ ₹80.00 each = ₹4,000
            // Total Stock: 150 units, Total Value: ₹9,000 => New WAC = 9000 / 150 = ₹60.00
            decimal currentStock = 100m;
            decimal currentWac = 50.00m;
            decimal acceptedQty = 50m;
            decimal landedCost = 80.00m;

            decimal newWac = valuationEngine.CalculateMovingWac(currentStock, currentWac, acceptedQty, landedCost);

            Assert.Equal(60.0000m, newWac);
        }

        [Fact]
        public async Task ProcessGrnInventoryValuation_WritesStockTransactionAndRecalculatesWac()
        {
            using var db = CreateDbContext();
            var valuationEngine = new InventoryValuationEngine(db);

            // Seed Raw Material: initial 100 units @ ₹100
            var rawMat = new RawMaterial
            {
                Id = 1,
                MaterialCode = "RM-STEEL-001",
                MaterialName = "Cold Rolled Steel Coil",
                AvailableStock = 100m,
                UnitCost = 100.00m,
                CurrentValue = 10000.00m,
                WarehouseId = 1
            };
            db.RawMaterials.Add(rawMat);

            // Seed GRN with accepted item: 100 units @ ₹120 landed cost
            var grn = new GoodsReceipt
            {
                Id = 1,
                GRNNumber = "GRN-2026-00001",
                PurchaseOrderId = 10,
                PurchaseOrderNumber = "PO-2026-00010",
                VendorId = 5,
                VendorName = "Tata Steel Ltd",
                ReceiptDate = DateTime.UtcNow,
                Status = GoodsReceiptStatus.Submitted,
                QualityStatus = QualityInspectionStatus.Passed,
                Items = new List<GoodsReceiptItem>
                {
                    new GoodsReceiptItem
                    {
                        Id = 101,
                        PurchaseOrderLineId = 501,
                        ItemId = 1,
                        ItemName = "Cold Rolled Steel Coil",
                        OrderedQuantity = 100m,
                        ReceivedQuantity = 100m,
                        AcceptedQuantity = 100m,
                        PoRate = 120.00m,
                        LandedCostPerUnit = 120.00m
                    }
                }
            };
            db.GoodsReceipts.Add(grn);
            await db.SaveChangesAsync();

            // Execute Perpetual Inventory Valuation
            var result = await valuationEngine.ProcessGrnInventoryValuationAsync(grn.Id, "user-wh-01");

            Assert.Equal(1, result.ItemsValuedCount);
            Assert.Equal(12000.00m, result.TotalValuationAmount);

            // Verify Raw Material Stock & WAC updated:
            // Previous: 100 @ 100 = 10,000. New: 100 @ 120 = 12,000. Total: 200 @ 110 = 22,000.
            var updatedMat = await db.RawMaterials.FindAsync(1);
            Assert.NotNull(updatedMat);
            Assert.Equal(200m, updatedMat.AvailableStock);
            Assert.Equal(110.0000m, updatedMat.UnitCost);
            Assert.Equal(22000.00m, updatedMat.CurrentValue);

            // Verify StockTransaction created with Dr Raw Material / Cr GRN/IR Clearing
            var txn = await db.StockTransactions.FirstOrDefaultAsync(t => t.ReferenceId == grn.Id);
            Assert.NotNull(txn);
            Assert.Equal(StockTxnType.PurchaseReceipt, txn.TransactionType);
            Assert.Equal(StockReferenceType.GRN, txn.ReferenceType);
            Assert.Equal(100m, txn.Quantity);
            Assert.Equal(200m, txn.BalanceAfter);
            Assert.Contains("GRN/IR Clearing", txn.Remarks);
        }

        [Fact]
        public async Task ThreeWayMatch_Passes_WhenRatesAndQuantitiesAlignWithinTolerance()
        {
            using var db = CreateDbContext();
            var matchEngine = new ThreeWayMatchingEngine(db);

            // Seed PO with Line
            var po = new PurchaseOrder
            {
                Id = 1,
                PurchaseOrderNumber = "PO-2026-00001",
                VendorId = 10,
                VendorName = "Precision Fasteners Ltd",
                Lines = new List<PurchaseOrderLine>
                {
                    new PurchaseOrderLine
                    {
                        Id = 1,
                        ItemName = "Hex Bolt M12x50",
                        Quantity = 1000m,
                        Rate = 15.00m,
                        GstRatePercentage = 18.00m,
                        BilledQuantity = 0m
                    }
                }
            };
            db.PurchaseOrders.Add(po);

            // Seed GRN with Accepted quantity
            var grn = new GoodsReceipt
            {
                Id = 1,
                GRNNumber = "GRN-2026-00001",
                PurchaseOrderId = 1,
                Status = GoodsReceiptStatus.Completed,
                Items = new List<GoodsReceiptItem>
                {
                    new GoodsReceiptItem
                    {
                        Id = 1,
                        PurchaseOrderLineId = 1,
                        ItemName = "Hex Bolt M12x50",
                        ReceivedQuantity = 1000m,
                        AcceptedQuantity = 1000m,
                        BilledQuantity = 0m
                    }
                }
            };
            db.GoodsReceipts.Add(grn);
            await db.SaveChangesAsync();

            // Dry-run 3-way match with matching invoice
            var billReq = new PurchaseBillCreateRequestDto
            {
                PurchaseOrderId = 1,
                GRNId = 1,
                Lines = new List<PurchaseBillLineReqDto>
                {
                    new PurchaseBillLineReqDto
                    {
                        PurchaseOrderLineId = 1,
                        GoodsReceiptItemId = 1,
                        ItemName = "Hex Bolt M12x50",
                        Quantity = 1000m,
                        UnitPrice = 15.00m,
                        TaxPercent = 18.00m
                    }
                }
            };

            var matchResult = await matchEngine.EvaluateMatchAsync(billReq);

            Assert.True(matchResult.IsMatched);
            Assert.False(matchResult.HasVarianceWarning);
            Assert.False(matchResult.IsBlocked);
            Assert.Equal("Matched", matchResult.Status);
        }

        [Fact]
        public async Task ThreeWayMatch_HardBlocks_WhenBilledQuantityExceedsGrnAccepted()
        {
            using var db = CreateDbContext();
            var matchEngine = new ThreeWayMatchingEngine(db);

            var po = new PurchaseOrder
            {
                Id = 2,
                PurchaseOrderNumber = "PO-2026-00002",
                VendorId = 10,
                Lines = new List<PurchaseOrderLine>
                {
                    new PurchaseOrderLine { Id = 2, ItemName = "Industrial Valve", Quantity = 10m, Rate = 5000m, GstRatePercentage = 18m }
                }
            };
            db.PurchaseOrders.Add(po);

            var grn = new GoodsReceipt
            {
                Id = 2,
                PurchaseOrderId = 2,
                Status = GoodsReceiptStatus.Completed,
                Items = new List<GoodsReceiptItem>
                {
                    new GoodsReceiptItem { Id = 2, PurchaseOrderLineId = 2, ItemName = "Industrial Valve", AcceptedQuantity = 10m, BilledQuantity = 0m }
                }
            };
            db.GoodsReceipts.Add(grn);
            await db.SaveChangesAsync();

            // Attempt to bill 12 units against 10 accepted units
            var billReq = new PurchaseBillCreateRequestDto
            {
                PurchaseOrderId = 2,
                GRNId = 2,
                Lines = new List<PurchaseBillLineReqDto>
                {
                    new PurchaseBillLineReqDto
                    {
                        PurchaseOrderLineId = 2,
                        GoodsReceiptItemId = 2,
                        ItemName = "Industrial Valve",
                        Quantity = 12m, // Excess!
                        UnitPrice = 5000m,
                        TaxPercent = 18m
                    }
                }
            };

            var matchResult = await matchEngine.EvaluateMatchAsync(billReq);

            Assert.False(matchResult.IsMatched);
            Assert.True(matchResult.IsBlocked);
            Assert.Equal("Blocked", matchResult.Status);
            Assert.True(matchResult.LineResults[0].ExceedsBilledLimit);
        }

        [Fact]
        public async Task ThreeWayMatch_FlagsPriceVarianceHold_WhenBillRateExceedsPoRate()
        {
            using var db = CreateDbContext();
            var matchEngine = new ThreeWayMatchingEngine(db);

            var po = new PurchaseOrder
            {
                Id = 3,
                PurchaseOrderNumber = "PO-2026-00003",
                VendorId = 10,
                Lines = new List<PurchaseOrderLine>
                {
                    new PurchaseOrderLine { Id = 3, ItemName = "Copper Ingot", Quantity = 100m, Rate = 700m, GstRatePercentage = 18m }
                }
            };
            db.PurchaseOrders.Add(po);

            var grn = new GoodsReceipt
            {
                Id = 3,
                PurchaseOrderId = 3,
                Status = GoodsReceiptStatus.Completed,
                Items = new List<GoodsReceiptItem>
                {
                    new GoodsReceiptItem { Id = 3, PurchaseOrderLineId = 3, ItemName = "Copper Ingot", AcceptedQuantity = 100m, BilledQuantity = 0m }
                }
            };
            db.GoodsReceipts.Add(grn);
            await db.SaveChangesAsync();

            // Vendor invoiced at ₹750 instead of PO rate ₹700 (7.14% variance > 0.5% tolerance)
            var billReq = new PurchaseBillCreateRequestDto
            {
                PurchaseOrderId = 3,
                GRNId = 3,
                Lines = new List<PurchaseBillLineReqDto>
                {
                    new PurchaseBillLineReqDto
                    {
                        PurchaseOrderLineId = 3,
                        GoodsReceiptItemId = 3,
                        ItemName = "Copper Ingot",
                        Quantity = 100m,
                        UnitPrice = 750m, // Price variance!
                        TaxPercent = 18m
                    }
                }
            };

            var matchResult = await matchEngine.EvaluateMatchAsync(billReq);

            Assert.False(matchResult.IsMatched);
            Assert.True(matchResult.HasVarianceWarning);
            Assert.False(matchResult.IsBlocked);
            Assert.Equal("Warning", matchResult.Status);
            Assert.True(matchResult.LineResults[0].RateVariancePercent > 0.5m);
        }

        [Fact]
        public async Task ManagerialOverride_AuthorizesPostingOfVarianceHoldBill()
        {
            using var db = CreateDbContext();
            var matchEngine = new ThreeWayMatchingEngine(db);

            var bill = new PurchaseBill
            {
                Id = 5,
                BillNumber = "BILL-2026-00005",
                InvoiceNumber = "INV-789",
                VendorId = 12,
                VendorName = "Apex Metalworks",
                Status = PurchaseBillStatus.VarianceHold,
                ThreeWayMatchStatus = ThreeWayMatchStatus.PriceVarianceHold,
                IsThreeWayMatched = false
            };
            db.PurchaseBills.Add(bill);
            await db.SaveChangesAsync();

            bool overridden = await matchEngine.AuthorizeVarianceOverrideAsync(
                bill.Id, 
                "Commodity price index hike verified and approved per contract clause 4.2", 
                "manager-cfo-01");

            Assert.True(overridden);

            var updatedBill = await db.PurchaseBills.FindAsync(5);
            Assert.NotNull(updatedBill);
            Assert.Equal(ThreeWayMatchStatus.ManuallyApproved, updatedBill.ThreeWayMatchStatus);
            Assert.True(updatedBill.IsThreeWayMatched);
            Assert.Equal(PurchaseBillStatus.Approved, updatedBill.Status);
            Assert.Equal("manager-cfo-01", updatedBill.VarianceOverriddenBy);

            // Confirm audit log was created
            var auditLog = await db.ThreeWayMatchAuditLogs.FirstOrDefaultAsync(a => a.PurchaseBillId == 5 && a.CheckType == "ManagerialOverride");
            Assert.NotNull(auditLog);
            Assert.Equal("manager-cfo-01", auditLog.OverriddenBy);
        }

        [Fact]
        public async Task OverDeliveryTolerance_RejectsDiscreteItemExcess_And_AllowsBulkCommodityWithin2Percent()
        {
            using var db = CreateDbContext();
            var valuationEngine = new InventoryValuationEngine(db);
            var numberingService = new GoodsReceiptNumberingService(db);
            var grService = new GoodsReceiptService(db, numberingService, valuationEngine);

            var po = new PurchaseOrder
            {
                Id = 20,
                PurchaseOrderNumber = "PO-2026-00020",
                VendorId = 15,
                VendorName = "Global Supplies",
                Status = PurchaseOrderStatus.Approved,
                Lines = new List<PurchaseOrderLine>
                {
                    new PurchaseOrderLine
                    {
                        Id = 201,
                        ItemName = "High-precision Gearbox",
                        Quantity = 10m, // Discrete part: 0% tolerance
                        OverDeliveryTolerancePercent = 0.0m,
                        Rate = 50000m
                    },
                    new PurchaseOrderLine
                    {
                        Id = 202,
                        ItemName = "Sand Bulk Cargo",
                        Quantity = 1000m, // Bulk commodity: 2% tolerance
                        OverDeliveryTolerancePercent = 2.0m,
                        Rate = 120m
                    }
                }
            };
            db.PurchaseOrders.Add(po);
            await db.SaveChangesAsync();

            // 1. Attempting 11 units on discrete item with 0% tolerance should throw
            var discreteReq = new GoodsReceiptCreateRequestDto
            {
                PurchaseOrderId = 20,
                Items = new List<GoodsReceiptItemDto>
                {
                    new GoodsReceiptItemDto
                    {
                        PurchaseOrderLineId = "201",
                        ItemName = "High-precision Gearbox",
                        OrderedQuantity = 10m,
                        ReceivedQuantity = 11m // 10% excess on 0% tolerance
                    }
                }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => grService.CreateAsync(discreteReq, "user-gate-01"));

            // 2. Receiving 1015 MT on bulk item with 2% tolerance (max 1020 MT) should succeed
            var bulkReq = new GoodsReceiptCreateRequestDto
            {
                PurchaseOrderId = 20,
                Items = new List<GoodsReceiptItemDto>
                {
                    new GoodsReceiptItemDto
                    {
                        PurchaseOrderLineId = "202",
                        ItemName = "Sand Bulk Cargo",
                        OrderedQuantity = 1000m,
                        ReceivedQuantity = 1015m // Within 2% tolerance (1000 * 1.02 = 1020)
                    }
                }
            };

            var grn = await grService.CreateAsync(bulkReq, "user-gate-01");
            Assert.NotNull(grn);
            Assert.Equal(1015m, grn.Items[0].ReceivedQuantity);
        }

        [Fact]
        public async Task MsmeSection43Bh_Calculates15DaysWithoutAgreement_And_45DaysWithAgreement()
        {
            using var db = CreateDbContext();
            var numberingService = new PurchaseBillNumberingService(db);
            var matchEngine = new ThreeWayMatchingEngine(db);
            var billService = new PurchaseBillService(db, numberingService, matchEngine);

            // Seed Micro Vendor
            var microVendor = new Vendor
            {
                Id = 30,
                Name = "Micro Precision Tooling",
                IsMsmeRegistered = true,
                MsmeCategory = MsmeCategory.Micro,
                PAN = "AAACM1234F"
            };
            db.Vendors.Add(microVendor);
            await db.SaveChangesAsync();

            var invoiceDate = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

            // Case A: Verbal / No written agreement => Capped strictly at 15 days
            var billReqNoAgreement = new PurchaseBillCreateRequestDto
            {
                VendorId = 30,
                VendorName = "Micro Precision Tooling",
                InvoiceDate = invoiceDate,
                PaymentTerms = "Immediate", // Immediate / verbal
                Lines = new List<PurchaseBillLineReqDto>
                {
                    new PurchaseBillLineReqDto { ItemName = "Tool Bit", Quantity = 10m, UnitPrice = 100m, TaxPercent = 18m }
                }
            };

            var billA = await billService.CreatePurchaseBillAsync(billReqNoAgreement, "user-ap-01");
            Assert.True(billA.IsMsmeSubjectTo43B);
            Assert.Equal(15, billA.MsmeAllowedDays);
            Assert.Equal(invoiceDate.AddDays(15), billA.MsmeDueDate);

            // Case B: Written agreement Net 30 => Capped at 45 days
            var billReqWithAgreement = new PurchaseBillCreateRequestDto
            {
                VendorId = 30,
                VendorName = "Micro Precision Tooling",
                InvoiceDate = invoiceDate,
                PaymentTerms = "Net 30 Days",
                Lines = new List<PurchaseBillLineReqDto>
                {
                    new PurchaseBillLineReqDto { ItemName = "Tool Bit", Quantity = 10m, UnitPrice = 100m, TaxPercent = 18m }
                }
            };

            var billB = await billService.CreatePurchaseBillAsync(billReqWithAgreement, "user-ap-01");
            Assert.True(billB.IsMsmeSubjectTo43B);
            Assert.Equal(45, billB.MsmeAllowedDays);
            Assert.Equal(invoiceDate.AddDays(45), billB.MsmeDueDate);
        }

        [Fact]
        public async Task TdsSection194Q_DeductsZeroPointOnePercent_WhenCumulativeTurnoverCrosses50Lakhs()
        {
            using var db = CreateDbContext();
            var numberingService = new PurchaseBillNumberingService(db);
            var matchEngine = new ThreeWayMatchingEngine(db);
            var billService = new PurchaseBillService(db, numberingService, matchEngine);

            var vendor = new Vendor
            {
                Id = 40,
                Name = "Mega Industrial Corporation",
                PAN = "ABCDE1234F" // Valid PAN => 0.1% TDS
            };
            db.Vendors.Add(vendor);

            // Seed prior FY purchases totalling ₹49,00,000 (just ₹1 Lakh short of 50 Lakhs)
            var priorBill = new PurchaseBill
            {
                Id = 100,
                BillNumber = "BILL-PRIOR-01",
                VendorId = 40,
                VendorName = "Mega Industrial Corporation",
                InvoiceDate = DateTime.UtcNow,
                SubTotal = 4900000m,
                GrandTotal = 4900000m,
                Status = PurchaseBillStatus.Posted
            };
            db.PurchaseBills.Add(priorBill);
            await db.SaveChangesAsync();

            // New Bill for ₹3,00,000. Cumulative = ₹52,00,000 => Excess over 50L = ₹2,00,000.
            // TDS @ 0.1% on ₹2,00,000 = ₹200.00
            var newBillReq = new PurchaseBillCreateRequestDto
            {
                VendorId = 40,
                VendorName = "Mega Industrial Corporation",
                InvoiceDate = DateTime.UtcNow,
                Lines = new List<PurchaseBillLineReqDto>
                {
                    new PurchaseBillLineReqDto { ItemName = "Steel Plate", Quantity = 100m, UnitPrice = 3000m, TaxPercent = 0m }
                }
            };

            var createdBill = await billService.CreatePurchaseBillAsync(newBillReq, "user-ap-01");

            Assert.True(createdBill.IsTds194QApplicable);
            Assert.Equal(0.10m, createdBill.Tds194QPercentage);
            Assert.Equal(200.00m, createdBill.Tds194QAmount);
            Assert.Equal(300000m - 200m, createdBill.GrandTotal);
        }

        [Fact]
        public async Task MultiGrnConsolidation_CreatesUnifiedBillAcrossAcceptedGrns()
        {
            using var db = CreateDbContext();
            var numberingService = new PurchaseBillNumberingService(db);
            var matchEngine = new ThreeWayMatchingEngine(db);
            var billService = new PurchaseBillService(db, numberingService, matchEngine);

            var vendor = new Vendor { Id = 50, Name = "Allied Polymers Pvt Ltd" };
            db.Vendors.Add(vendor);

            // Seed GRN 1: 50 units
            var grn1 = new GoodsReceipt
            {
                Id = 11,
                GRNNumber = "GRN-2026-00011",
                VendorId = 50,
                Status = GoodsReceiptStatus.Completed,
                Items = new List<GoodsReceiptItem>
                {
                    new GoodsReceiptItem { Id = 111, ItemName = "Polymer Pellets", AcceptedQuantity = 50m, PoRate = 100m, BilledQuantity = 0m }
                }
            };
            // Seed GRN 2: 30 units
            var grn2 = new GoodsReceipt
            {
                Id = 12,
                GRNNumber = "GRN-2026-00012",
                VendorId = 50,
                Status = GoodsReceiptStatus.Completed,
                Items = new List<GoodsReceiptItem>
                {
                    new GoodsReceiptItem { Id = 112, ItemName = "Polymer Pellets", AcceptedQuantity = 30m, PoRate = 100m, BilledQuantity = 0m }
                }
            };
            db.GoodsReceipts.AddRange(grn1, grn2);
            await db.SaveChangesAsync();

            // Consolidate both GRNs into a single invoice
            var consolReq = new MultiGrnBillBuildRequestDto
            {
                VendorId = 50,
                GrnIds = new List<int> { 11, 12 }
            };

            var bill = await billService.BuildFromGrnsAsync(consolReq, "user-ap-01");

            Assert.NotNull(bill);
            Assert.Equal(2, bill.Lines.Count);
            Assert.Equal(80m, bill.Lines.Sum(l => l.Quantity));
            Assert.Equal(8000m, bill.SubTotal);
            Assert.Contains("11", bill.ConsolidatedGrnIds);
            Assert.Contains("12", bill.ConsolidatedGrnIds);
        }

        [Fact]
        public async Task PostPurchaseBill_WritesVendorLedgerEntry_And_ClearsLiability()
        {
            using var db = CreateDbContext();
            var numberingService = new PurchaseBillNumberingService(db);
            var matchEngine = new ThreeWayMatchingEngine(db);
            var billService = new PurchaseBillService(db, numberingService, matchEngine);

            var vendor = new Vendor { Id = 60, Name = "Dynamic Logistics" };
            db.Vendors.Add(vendor);

            var bill = new PurchaseBill
            {
                Id = 60,
                BillNumber = "BILL-2026-00060",
                VendorId = 60,
                VendorName = "Dynamic Logistics",
                Status = PurchaseBillStatus.Approved,
                ThreeWayMatchStatus = ThreeWayMatchStatus.Matched,
                IsThreeWayMatched = true,
                SubTotal = 50000m,
                GrandTotal = 59000m,
                TdsAmount = 50m
            };
            db.PurchaseBills.Add(bill);
            await db.SaveChangesAsync();

            var postedBill = await billService.PostPurchaseBillAsync(60, "Fiscal liability verified", "user-ap-01");

            Assert.NotNull(postedBill);
            Assert.Equal(PurchaseBillStatus.Posted, postedBill.Status);

            // Verify VendorLedgerEntry was created
            var ledger = await db.VendorLedgerEntries.FirstOrDefaultAsync(l => l.PurchaseBillId == 60);
            Assert.NotNull(ledger);
            Assert.Equal(59000m, ledger.CreditAmount);
            Assert.Equal("Bill", ledger.EntryType);
            Assert.Contains("GRN/IR Clearing", ledger.Narration);
        }
    }
}

