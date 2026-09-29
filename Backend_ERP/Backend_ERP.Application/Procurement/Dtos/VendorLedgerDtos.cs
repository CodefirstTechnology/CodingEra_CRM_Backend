using System;
using System.Collections.Generic;

namespace ERP.Application.Procurement.Dtos
{
    public class VendorAgingSummaryDto
    {
        public int VendorId { get; set; }

        public string VendorName { get; set; } = string.Empty;

        public decimal TotalOutstanding { get; set; }

        public decimal CurrentAmount { get; set; }

        public decimal Aging1To30 { get; set; }

        public decimal Aging31To60 { get; set; }

        public decimal Aging61To90 { get; set; }

        public decimal Aging90Plus { get; set; }
    }

    public class VendorLedgerStatementItemDto
    {
        public int Id { get; set; }

        public DateTime EntryDate { get; set; }

        public string VoucherNumber { get; set; } = string.Empty;

        public string EntryType { get; set; } = string.Empty;

        public string? ReferenceNumber { get; set; }

        public decimal DebitAmount { get; set; }

        public decimal CreditAmount { get; set; }

        public decimal RunningBalance { get; set; }

        public string? Narration { get; set; }
    }

    public class VendorLedgerStatementResultDto
    {
        public int VendorId { get; set; }

        public string VendorName { get; set; } = string.Empty;

        public decimal OpeningBalance { get; set; }

        public decimal TotalDebit { get; set; }

        public decimal TotalCredit { get; set; }

        public decimal NetBalance { get; set; }

        public List<VendorLedgerStatementItemDto> Entries { get; set; } = new();
    }

    public class CreateVendorPaymentDto
    {
        public int VendorId { get; set; }

        public string PaymentNumber { get; set; } = string.Empty;

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        public string PaymentMethod { get; set; } = "NEFT/RTGS";

        public int? BankAccountId { get; set; }

        public decimal TotalAmount { get; set; }

        public string? ReferenceNumber { get; set; }

        public List<PaymentAllocationDto> Allocations { get; set; } = new();
    }

    public class PaymentAllocationDto
    {
        public int PurchaseBillId { get; set; }

        public decimal AllocatedAmount { get; set; }
    }

    public class VendorPaymentDto
    {
        public int Id { get; set; }

        public int VendorId { get; set; }

        public string PaymentNumber { get; set; } = string.Empty;

        public DateTime PaymentDate { get; set; }

        public string PaymentMethod { get; set; } = string.Empty;

        public int? BankAccountId { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal UnallocatedAmount { get; set; }

        public string? ReferenceNumber { get; set; }

        public string Status { get; set; } = "Cleared";

        public DateTime CreatedAt { get; set; }

        public List<PaymentAllocationResultDto> Allocations { get; set; } = new();
    }

    public class PaymentAllocationResultDto
    {
        public int Id { get; set; }

        public int VendorPaymentId { get; set; }

        public int PurchaseBillId { get; set; }

        public string BillNumber { get; set; } = string.Empty;

        public decimal AllocatedAmount { get; set; }

        public DateTime AllocatedAt { get; set; }
    }
}
