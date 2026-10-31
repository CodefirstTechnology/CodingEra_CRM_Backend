using System;
using System.Collections.Generic;

namespace ERP.Domain.Procurement
{
    public class Vendor
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "default";

        public string VendorCode { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string LegalName { get; set; } = string.Empty;

        public string TradeName { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public VendorType VendorType { get; set; } = VendorType.Manufacturer;

        public string GSTIN { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string Gstin
        {
            get => GSTIN;
            set => GSTIN = value;
        }

        public string PAN { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string Pan
        {
            get => PAN;
            set => PAN = value;
        }

        public string? Tan { get; set; }

        public string? Cin { get; set; }

        public string TaxIdentificationNumber { get; set; } = string.Empty;

        // MSME Statutory Attributes
        public bool IsMsmeRegistered { get; set; }

        public string? MsmeRegistrationNumber { get; set; }

        public MsmeCategory MsmeCategory { get; set; } = MsmeCategory.None;

        // Contact & Communication
        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string AlternatePhone { get; set; } = string.Empty;

        public string Website { get; set; } = string.Empty;

        // Commercial Terms
        public int PaymentTermsDays { get; set; } = 30;

        public int? PaymentTermId { get; set; }

        public VendorPaymentTerm? PaymentTerm { get; set; }

        public string Currency { get; set; } = "INR";

        public decimal CreditLimit { get; set; }

        public decimal TotalOpenCommitment { get; set; }

        public string? Incoterms { get; set; }

        // Operational & Quality Rating
        public decimal RatingScore { get; set; } = 100.00m;

        public VendorStatus Status { get; set; } = VendorStatus.Draft;

        public bool IsKycVerified { get; set; }

        public string? ApprovalToken { get; set; }

        public string Notes { get; set; } = string.Empty;

        // Audit Trail
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }

        // PostgreSQL xmin Concurrency Token
        public uint ConcurrencyVersion { get; set; }

        // Navigation Collections
        public List<VendorContact> Contacts { get; set; } = new();

        public List<VendorAddress> Addresses { get; set; } = new();

        public List<VendorBankAccount> BankAccounts { get; set; } = new();

        public List<VendorItemCatalog> ItemCatalogs { get; set; } = new();

        public List<VendorPerformanceScore> PerformanceScores { get; set; } = new();

        public List<VendorApprovalHistory> ApprovalHistories { get; set; } = new();

        public VendorCompliance? Compliance { get; set; }

        public List<VendorStatusHistory> StatusHistory { get; set; } = new();
    }
}
