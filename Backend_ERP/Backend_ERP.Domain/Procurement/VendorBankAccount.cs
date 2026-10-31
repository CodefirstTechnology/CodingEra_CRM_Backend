using System;

namespace ERP.Domain.Procurement
{
    public class VendorBankAccount
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "default";

        public int VendorId { get; set; }

        public Vendor? Vendor { get; set; }

        public string AccountNumber { get; set; } = string.Empty;

        public string IfscCode { get; set; } = string.Empty;

        public string BankName { get; set; } = string.Empty;

        public string Branch { get; set; } = string.Empty;

        public string BeneficiaryName { get; set; } = string.Empty;

        public string AccountType { get; set; } = "Current";

        public bool IsPrimary { get; set; }

        public BankAccountVerificationStatus VerificationStatus { get; set; } = BankAccountVerificationStatus.Pending;

        public string? CancelledChequeUrl { get; set; }

        public DateTime? VerifiedAt { get; set; }

        public string? VerifiedBy { get; set; }
    }
}

