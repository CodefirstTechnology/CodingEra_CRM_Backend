using System;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using Xunit;

namespace Backend_ERP.Tests
{
    public class PurchaseBillContractTests
    {
        [Theory]
        [InlineData(PurchaseBillStatus.Draft, PurchaseBillStatus.Approved, true)]
        [InlineData(PurchaseBillStatus.Approved, PurchaseBillStatus.Posted, true)]
        [InlineData(PurchaseBillStatus.Posted, PurchaseBillStatus.Paid, true)]
        [InlineData(PurchaseBillStatus.Draft, PurchaseBillStatus.Void, true)]
        [InlineData(PurchaseBillStatus.Paid, PurchaseBillStatus.Draft, false)]
        public void PurchaseBillRules_validates_status_transitions(PurchaseBillStatus current, PurchaseBillStatus target, bool expected)
        {
            Assert.Equal(expected, PurchaseBillRules.CanTransition(current, target));
        }

        [Fact]
        public void PurchaseBillRules_calculates_totals_accurately()
        {
            var lines = new[]
            {
                (Quantity: 10m, UnitPrice: 100m, DiscountAmount: 50m, TaxPercent: 18m),
                (Quantity: 5m, UnitPrice: 200m, DiscountAmount: 0m, TaxPercent: 18m)
            };

            var totals = PurchaseBillRules.CalculateTotals(lines);

            Assert.Equal(2000m, totals.SubTotal);
            Assert.Equal(50m, totals.DiscountTotal);
            Assert.Equal(351m, totals.TaxTotal); // (950 + 1000) * 18% = 351
            Assert.Equal(2301m, totals.GrandTotal);
        }

        [Fact]
        public void PurchaseBillDto_exposes_expected_json_contract_properties()
        {
            var dto = new PurchaseBillDto
            {
                Id = 1,
                BillNumber = "BILL-2026-000001",
                InvoiceNumber = "INV-2026-991",
                VendorId = 10,
                VendorName = "Acme Supplies Ltd",
                PurchaseOrderNumber = "PO-2026-000001",
                GRNNumber = "GRN-2026-000001",
                SubTotal = 2000m,
                DiscountTotal = 50m,
                TaxTotal = 351m,
                GrandTotal = 2301m,
                PaidAmount = 0m,
                BalanceAmount = 2301m,
                PaymentStatus = PurchaseBillPaymentStatus.Unpaid,
                Status = PurchaseBillStatus.Draft
            };

            Assert.Equal("BILL-2026-000001", dto.BillNumber);
            Assert.Equal("INV-2026-991", dto.InvoiceNumber);
            Assert.Equal("Acme Supplies Ltd", dto.VendorName);
            Assert.Equal("PO-2026-000001", dto.PurchaseOrderNumber);
            Assert.Equal("GRN-2026-000001", dto.GRNNumber);
            Assert.Equal(2301m, dto.GrandTotal);
            Assert.Equal(PurchaseBillPaymentStatus.Unpaid, dto.PaymentStatus);
            Assert.Equal(PurchaseBillStatus.Draft, dto.Status);
        }
    }
}
