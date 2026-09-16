using System;

namespace ERP.Domain.Sales
{
    public class AdvancePaymentReceiptVoucher
    {
        public int Id { get; set; }

        public string VoucherNumber { get; set; } = string.Empty;

        public int AdvancePaymentId { get; set; }

        public AdvancePayment AdvancePayment { get; set; } = null!;

        public int CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string PlaceOfSupply { get; set; } = string.Empty;

        public bool IsInterState { get; set; }

        public decimal TaxableAmount { get; set; }

        public decimal CgstRate { get; set; }

        public decimal CgstAmount { get; set; }

        public decimal SgstRate { get; set; }

        public decimal SgstAmount { get; set; }

        public decimal IgstRate { get; set; }

        public decimal IgstAmount { get; set; }

        public decimal TotalVoucherAmount { get; set; }

        public DateOnly VoucherDate { get; set; }

        public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;
    }
}
