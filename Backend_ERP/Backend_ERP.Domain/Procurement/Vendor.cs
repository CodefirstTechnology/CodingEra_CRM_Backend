using System;
using System.Collections.Generic;

namespace ERP.Domain.Procurement
{
    public class Vendor
    {
        public int Id { get; set; }

        public string VendorCode { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string LegalName { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string GSTIN { get; set; } = string.Empty;

        public string PAN { get; set; } = string.Empty;

        public string TaxIdentificationNumber { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string AlternatePhone { get; set; } = string.Empty;

        public string Website { get; set; } = string.Empty;

        public int? PaymentTermId { get; set; }

        public VendorPaymentTerm? PaymentTerm { get; set; }

        public decimal CreditLimit { get; set; }

        public VendorStatus Status { get; set; } = VendorStatus.PendingApproval;

        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }

        public List<VendorContact> Contacts { get; set; } = new();

        public List<VendorAddress> Addresses { get; set; } = new();

        public VendorCompliance? Compliance { get; set; }

        public List<VendorStatusHistory> StatusHistory { get; set; } = new();
    }
}
