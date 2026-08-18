using System;
using ERP.Application.Procurement.Dtos;
using Xunit;

namespace Backend_ERP.Tests
{
    public class ProcurementReportsContractTests
    {
        [Fact]
        public void ProcurementReportKpisDto_exposes_expected_json_contract_properties()
        {
            var kpis = new ProcurementReportKpisDto
            {
                TotalPurchaseOrders = 25,
                PendingApprovals = 3,
                CompletedPurchaseOrders = 15,
                OpenPurchaseOrders = 7,
                TotalGrns = 18,
                CompletedGrns = 14,
                PurchaseValue = 450000m,
                GoodsReceivedPercent = 88.5m,
                AverageApprovalTimeHours = 4.2,
                AverageReceiptTimeHours = 22.5
            };

            Assert.Equal(25, kpis.TotalPurchaseOrders);
            Assert.Equal(3, kpis.PendingApprovals);
            Assert.Equal(450000m, kpis.PurchaseValue);
            Assert.Equal(88.5m, kpis.GoodsReceivedPercent);
        }

        [Fact]
        public void AuditTrailEntryDto_exposes_expected_json_contract_properties()
        {
            var entry = new AuditTrailEntryDto
            {
                Id = "101",
                Module = "Purchase Order",
                Action = "Purchase Order Created",
                EntityId = 5,
                EntityNumber = "PO-2026-000005",
                User = "admin",
                Date = DateTime.UtcNow,
                Remarks = "Order submitted for approval"
            };

            Assert.Equal("101", entry.Id);
            Assert.Equal("Purchase Order", entry.Module);
            Assert.Equal("Purchase Order Created", entry.Action);
            Assert.Equal(5, entry.EntityId);
            Assert.Equal("PO-2026-000005", entry.EntityNumber);
        }

        [Fact]
        public void VendorPurchaseSummaryRowDto_exposes_expected_json_contract_properties()
        {
            var row = new VendorPurchaseSummaryRowDto
            {
                VendorName = "SafeGuard PPE Solutions",
                PurchaseOrderCount = 8,
                TotalValue = 125000m,
                CompletedCount = 6,
                OpenCount = 2,
                GrnCount = 7
            };

            Assert.Equal("SafeGuard PPE Solutions", row.VendorName);
            Assert.Equal(8, row.PurchaseOrderCount);
            Assert.Equal(125000m, row.TotalValue);
        }
    }
}
