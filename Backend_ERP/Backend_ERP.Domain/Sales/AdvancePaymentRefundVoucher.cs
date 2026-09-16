using System;

namespace ERP.Domain.Sales
{
    public class AdvancePaymentRefundVoucher
    {
        public int Id { get; set; }

        public string RefundVoucherNumber { get; set; } = string.Empty;

        public int AdvancePaymentId { get; set; }

        public AdvancePayment AdvancePayment { get; set; } = null!;

        public int ReceiptVoucherId { get; set; }

        public AdvancePaymentReceiptVoucher ReceiptVoucher { get; set; } = null!;

        public decimal RefundAmount { get; set; }

        public decimal TaxRefundedAmount { get; set; }

        public DateOnly RefundVoucherDate { get; set; }

        public string BankReferenceNumber { get; set; } = string.Empty;

        public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;
    }
}
