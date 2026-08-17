using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement.Dtos
{
    public class VendorListItemDto
    {
        public int Id { get; set; }

        [JsonPropertyName("vendorCode")]
        public string VendorCode { get => Code; set => Code = value; }

        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("vendorName")]
        public string VendorName { get => Name; set => Name = value; }

        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("companyName")]
        public string CompanyName { get => LegalName; set => LegalName = value; }

        public string LegalName { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("gstNumber")]
        public string GstNumber { get => GSTIN; set => GSTIN = value; }

        public string GSTIN { get; set; } = string.Empty;

        [JsonPropertyName("panNumber")]
        public string PanNumber { get => PAN; set => PAN = value; }

        public string PAN { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public VendorStatus Status { get; set; }

        public string PaymentTermName { get; set; } = string.Empty;

        public decimal CreditLimit { get; set; }

        public DateTime CreatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
    }

    public class VendorDto
    {
        public int Id { get; set; }

        [JsonPropertyName("vendorCode")]
        public string VendorCode { get => Code; set => Code = value; }

        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("vendorName")]
        public string VendorName { get => Name; set => Name = value; }

        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("companyName")]
        public string CompanyName { get => LegalName; set => LegalName = value; }

        public string LegalName { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("gstNumber")]
        public string GstNumber { get => GSTIN; set => GSTIN = value; }

        public string GSTIN { get; set; } = string.Empty;

        [JsonPropertyName("panNumber")]
        public string PanNumber { get => PAN; set => PAN = value; }

        public string PAN { get; set; } = string.Empty;

        public string TaxIdentificationNumber { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string AlternatePhone { get; set; } = string.Empty;

        public string Website { get; set; } = string.Empty;

        public int? PaymentTermId { get; set; }

        public VendorPaymentTermDto? PaymentTerm { get; set; }

        public decimal CreditLimit { get; set; }

        public VendorStatus Status { get; set; }

        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;

        public List<VendorContactDto> Contacts { get; set; } = new();

        public List<VendorAddressDto> Addresses { get; set; } = new();

        public VendorComplianceDto? Compliance { get; set; }

        public List<VendorStatusHistoryDto> StatusHistory { get; set; } = new();
    }

    public class VendorContactDto
    {
        public int Id { get; set; }

        public int VendorId { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Designation { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string AlternatePhone { get; set; } = string.Empty;

        public bool IsPrimary { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class VendorAddressDto
    {
        public int Id { get; set; }

        public int VendorId { get; set; }

        public VendorAddressType AddressType { get; set; }

        public string AddressLine1 { get; set; } = string.Empty;

        public string AddressLine2 { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string PostalCode { get; set; } = string.Empty;

        public string Country { get; set; } = "India";

        public bool IsPrimary { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class VendorComplianceDto
    {
        public int Id { get; set; }

        public int VendorId { get; set; }

        public string GSTIN { get; set; } = string.Empty;

        public string PAN { get; set; } = string.Empty;

        public string TaxIdentificationNumber { get; set; } = string.Empty;

        public bool GSTRegistered { get; set; }

        public bool MSMERegistered { get; set; }

        public string MSMENumber { get; set; } = string.Empty;

        public string CertificateNumber { get; set; } = string.Empty;

        public DateTime? CertificateExpiryDate { get; set; }

        public VendorComplianceStatus ComplianceStatus { get; set; }

        public string Remarks { get; set; } = string.Empty;
    }

    public class VendorPaymentTermDto
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int Days { get; set; }

        public decimal AdvancePercentage { get; set; }

        public bool IsActive { get; set; }
    }

    public class VendorStatusHistoryDto
    {
        public int Id { get; set; }

        public int VendorId { get; set; }

        public VendorStatus Status { get; set; }

        public VendorStatus? PreviousStatus { get; set; }

        public string User { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public DateTime Date { get; set; }
    }

    public class VendorListQueryDto
    {
        public string? Search { get; set; }

        public string? Status { get; set; }

        public string? Code { get; set; }

        public string? GSTIN { get; set; }

        public string? PAN { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string? SortBy { get; set; }

        public bool SortDescending { get; set; }
    }

    public class VendorCreateRequestDto
    {
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

        public decimal CreditLimit { get; set; }

        public VendorStatus Status { get; set; } = VendorStatus.PendingApproval;

        public string Notes { get; set; } = string.Empty;

        public List<VendorContactDto> Contacts { get; set; } = new();

        public List<VendorAddressDto> Addresses { get; set; } = new();

        public VendorComplianceDto? Compliance { get; set; }
    }

    public class VendorUpdateRequestDto
    {
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

        public decimal CreditLimit { get; set; }

        public string Notes { get; set; } = string.Empty;

        public List<VendorContactDto> Contacts { get; set; } = new();

        public List<VendorAddressDto> Addresses { get; set; } = new();

        public VendorComplianceDto? Compliance { get; set; }
    }

    public class VendorStatusUpdateRequestDto
    {
        public VendorStatus TargetStatus { get; set; }

        public string Remarks { get; set; } = string.Empty;
    }

    public class VendorPerformanceSummaryDto
    {
        public int VendorId { get; set; }

        public string VendorCode { get; set; } = string.Empty;

        public string VendorName { get; set; } = string.Empty;

        public int TotalPurchaseOrders { get; set; } = 0;

        public decimal TotalPurchaseValue { get; set; } = 0m;

        public double OnTimeDeliveryPercentage { get; set; } = 0.0;

        public double RejectionRatioPercentage { get; set; } = 0.0;

        public double LeadTimeCompliancePercentage { get; set; } = 0.0;

        public double PricingConsistencyScore { get; set; } = 0.0;

        public string Note { get; set; } = "Phase 1 Foundation: Real metrics will populate as Phase 2+ procurement transactions occur.";
    }
}
