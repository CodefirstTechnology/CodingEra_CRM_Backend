using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement.Dtos
{
    public class VendorListItemDto
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "default";

        [JsonPropertyName("vendorCode")]
        public string VendorCode { get => Code; set => Code = value; }

        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("vendorName")]
        public string VendorName { get => Name; set => Name = value; }

        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("companyName")]
        public string CompanyName { get => LegalName; set => LegalName = value; }

        public string LegalName { get; set; } = string.Empty;

        public string TradeName { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string VendorType { get; set; } = "Manufacturer";

        [JsonPropertyName("gstNumber")]
        public string GstNumber { get => GSTIN; set => GSTIN = value; }

        public string GSTIN { get; set; } = string.Empty;

        [JsonPropertyName("panNumber")]
        public string PanNumber { get => PAN; set => PAN = value; }

        public string PAN { get; set; } = string.Empty;

        public bool IsMsmeRegistered { get; set; }

        public string? MsmeRegistrationNumber { get; set; }

        public string MsmeCategory { get; set; } = "None";

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public VendorStatus Status { get; set; } = VendorStatus.Draft;

        public string PaymentTermName { get; set; } = string.Empty;

        public int PaymentTermsDays { get; set; } = 30;

        public string Currency { get; set; } = "INR";

        public decimal CreditLimit { get; set; }

        public decimal TotalOpenCommitment { get; set; }

        public decimal RatingScore { get; set; } = 100.00m;

        public bool IsKycVerified { get; set; }

        public string? ApprovalToken { get; set; }

        public DateTime CreatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public uint ConcurrencyVersion { get; set; }
    }

    public class VendorDto : VendorListItemDto
    {
        public string? Tan { get; set; }

        public string? Cin { get; set; }

        public string TaxIdentificationNumber { get; set; } = string.Empty;

        public string AlternatePhone { get; set; } = string.Empty;

        public string Website { get; set; } = string.Empty;

        public int? PaymentTermId { get; set; }

        public VendorPaymentTermDto? PaymentTerm { get; set; }

        public string? Incoterms { get; set; }

        public string Notes { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;

        public List<VendorContactDto> Contacts { get; set; } = new();

        public List<VendorAddressDto> Addresses { get; set; } = new();

        public List<VendorBankAccountDto> BankAccounts { get; set; } = new();

        public List<VendorItemCatalogDto> ItemCatalogs { get; set; } = new();

        public List<VendorPerformanceScoreDto> PerformanceScores { get; set; } = new();

        public List<VendorApprovalHistoryDto> ApprovalHistories { get; set; } = new();

        public VendorComplianceDto? Compliance { get; set; }

        public List<VendorStatusHistoryDto> StatusHistory { get; set; } = new();
    }

    public class VendorContactDto
    {
        public int Id { get; set; }

        public int VendorId { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Department { get; set; } = string.Empty;

        public string Designation { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string AlternatePhone { get; set; } = string.Empty;

        public string? Mobile { get; set; }

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

    public class VendorBankAccountDto
    {
        public int Id { get; set; }

        public int VendorId { get; set; }

        public string AccountNumber { get; set; } = string.Empty;

        public string IfscCode { get; set; } = string.Empty;

        public string BankName { get; set; } = string.Empty;

        public string Branch { get; set; } = string.Empty;

        public string BeneficiaryName { get; set; } = string.Empty;

        public string AccountType { get; set; } = "Current";

        public bool IsPrimary { get; set; }

        public string VerificationStatus { get; set; } = "Pending";

        public string? CancelledChequeUrl { get; set; }

        public DateTime? VerifiedAt { get; set; }

        public string? VerifiedBy { get; set; }
    }

    public class VendorItemCatalogDto
    {
        public int Id { get; set; }

        public int VendorId { get; set; }

        public string ItemType { get; set; } = "RawMaterial";

        public int ItemId { get; set; }

        public string ItemCode { get; set; } = string.Empty;

        public string? SupplierPartNumber { get; set; }

        public int StandardLeadTimeDays { get; set; } = 14;

        public decimal MinimumOrderQuantity { get; set; } = 1.0000m;

        public decimal OrderMultiple { get; set; } = 1.0000m;

        public string PackagingUnit { get; set; } = "Units";

        public decimal BaseUomConversionMultiplier { get; set; } = 1.000000m;

        public decimal UnitPriceSnapshot { get; set; }

        public string Currency { get; set; } = "INR";

        public DateOnly EffectiveFrom { get; set; }

        public DateOnly? EffectiveTo { get; set; }

        public bool IsApproved { get; set; } = true;

        public string? TechnicalRemarks { get; set; }
    }

    public class VendorItemCatalogCreateDto
    {
        public string ItemType { get; set; } = "RawMaterial";

        public int ItemId { get; set; }

        public string ItemCode { get; set; } = string.Empty;

        public string? SupplierPartNumber { get; set; }

        public int StandardLeadTimeDays { get; set; } = 14;

        public decimal MinimumOrderQuantity { get; set; } = 1.0000m;

        public decimal OrderMultiple { get; set; } = 1.0000m;

        public string PackagingUnit { get; set; } = "Units";

        public decimal BaseUomConversionMultiplier { get; set; } = 1.000000m;

        public decimal UnitPriceSnapshot { get; set; }

        public string Currency { get; set; } = "INR";

        public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

        public DateOnly? EffectiveTo { get; set; }

        public bool IsApproved { get; set; } = true;

        public string? TechnicalRemarks { get; set; }
    }

    public class VendorItemCatalogUpdateDto : VendorItemCatalogCreateDto
    {
    }

    public class SourcingGateValidationResultDto
    {
        public bool IsAllowed { get; set; }

        public string Reason { get; set; } = string.Empty;

        public VendorItemCatalogDto? CatalogItem { get; set; }
    }

    public class VendorLotInspectionIngestionDto
    {
        public int VendorId { get; set; }

        public int FiscalYear { get; set; } = DateTime.UtcNow.Year;

        public int Month { get; set; } = DateTime.UtcNow.Month;

        public decimal OtifPercentage { get; set; } = 100.00m;

        public decimal QualityPassRate { get; set; } = 100.00m;

        public decimal PriceVarianceScore { get; set; } = 100.00m;

        public decimal ComplianceScore { get; set; } = 100.00m;

        public int LotsReceived { get; set; } = 1;

        public int LotsRejected { get; set; } = 0;

        public bool HasCriticalSafetyDefect { get; set; }

        public string? CriticalDefectDescription { get; set; }
    }

    public class VendorPerformanceScoreDto
    {
        public int Id { get; set; }

        public int VendorId { get; set; }

        public string PeriodKey { get; set; } = string.Empty;

        public int FiscalYear { get; set; }

        public int Month { get; set; }

        public decimal OtifPercentage { get; set; }

        public decimal QualityPassRate { get; set; }

        public decimal PriceVarianceScore { get; set; }

        public decimal ComplianceScore { get; set; }

        public decimal BlendedRatingScore { get; set; }

        public int TotalLotsReceived { get; set; }

        public int TotalLotsRejected { get; set; }

        public decimal DefectPartsPerMillion { get; set; }

        public DateTime CalculatedAt { get; set; }
    }

    public class VendorApprovalHistoryDto
    {
        public int Id { get; set; }

        public int VendorId { get; set; }

        public string PreviousStatus { get; set; } = string.Empty;

        public string NewStatus { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public string Comments { get; set; } = string.Empty;

        public string ActionedByUserId { get; set; } = string.Empty;

        public string ActionedByUserName { get; set; } = string.Empty;

        public string? ApprovalToken { get; set; }

        public DateTime ActionedAt { get; set; }
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

        public string? VendorType { get; set; }

        public string? MsmeCategory { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string? SortBy { get; set; }

        public bool SortDescending { get; set; }
    }

    public class VendorCreateRequestDto
    {
        public string Name { get; set; } = string.Empty;

        public string LegalName { get; set; } = string.Empty;

        public string TradeName { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public VendorType VendorType { get; set; } = VendorType.Manufacturer;

        public string GSTIN { get; set; } = string.Empty;

        public string PAN { get; set; } = string.Empty;

        public string? Tan { get; set; }

        public string? Cin { get; set; }

        public string TaxIdentificationNumber { get; set; } = string.Empty;

        public bool IsMsmeRegistered { get; set; }

        public string? MsmeRegistrationNumber { get; set; }

        public MsmeCategory MsmeCategory { get; set; } = MsmeCategory.None;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string AlternatePhone { get; set; } = string.Empty;

        public string Website { get; set; } = string.Empty;

        public int? PaymentTermId { get; set; }

        public int PaymentTermsDays { get; set; } = 30;

        public string Currency { get; set; } = "INR";

        public decimal CreditLimit { get; set; }

        public string? Incoterms { get; set; }

        public VendorStatus Status { get; set; } = VendorStatus.Draft;

        public string Notes { get; set; } = string.Empty;

        public List<VendorContactDto> Contacts { get; set; } = new();

        public List<VendorAddressDto> Addresses { get; set; } = new();

        public List<VendorBankAccountDto> BankAccounts { get; set; } = new();

        public VendorComplianceDto? Compliance { get; set; }
    }

    public class VendorUpdateRequestDto : VendorCreateRequestDto
    {
        public uint ConcurrencyVersion { get; set; }
    }

    public class VendorKycApprovalRequestDto
    {
        public string Comments { get; set; } = string.Empty;

        public bool ApproveBankAccounts { get; set; } = true;
    }

    public class VendorStatusUpdateRequestDto
    {
        public VendorStatus TargetStatus { get; set; }

        public string Remarks { get; set; } = string.Empty;
    }

    public class VendorScoreResultDto
    {
        public int VendorId { get; set; }

        public string PeriodKey { get; set; } = string.Empty;

        public decimal OtifPercentage { get; set; }

        public decimal QualityPassRate { get; set; }

        public decimal PriceVarianceScore { get; set; }

        public decimal ComplianceScore { get; set; }

        public decimal BlendedRatingScore { get; set; }

        public bool TriggeredDisqualification { get; set; }
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

        public decimal BlendedRatingScore { get; set; } = 100.00m;

        public string Note { get; set; } = "Real-time P2P Telemetry Ingested from GRN and Incoming Inspection";
    }

    // Dashboard DTOs
    public class VendorDashboardKpiDto
    {
        public int TotalVendors { get; set; }

        public int ActiveVendors { get; set; }

        public int PendingKycCount { get; set; }

        public int OnHoldCount { get; set; }

        public decimal FleetOtifPercentage { get; set; }

        public decimal AverageQualityRating { get; set; }

        public decimal TotalOpenCommitments { get; set; }

        public int MsmeDueRiskCount { get; set; }

        public int CriticalSingleSourcedItems { get; set; }
    }

    public class VendorDashboardChartsDto
    {
        public List<CategorySpendSliceDto> SpendByCategory { get; set; } = new();

        public List<MonthlyOtifTrendPointDto> OtifTrend { get; set; } = new();

        public List<SupplierRadarPointDto> TopSuppliersRadar { get; set; } = new();

        public List<LeadTimeVarianceBarDto> CriticalLeadTimeVariance { get; set; } = new();
    }

    public class CategorySpendSliceDto
    {
        public string CategoryName { get; set; } = string.Empty;

        public decimal TotalSpend { get; set; }

        public decimal SpendPercentage { get; set; }
    }

    public class MonthlyOtifTrendPointDto
    {
        public string MonthLabel { get; set; } = string.Empty;

        public decimal OtifPercentage { get; set; }

        public decimal TargetSla { get; set; } = 95.00m;
    }

    public class SupplierRadarPointDto
    {
        public string VendorName { get; set; } = string.Empty;

        public decimal QualityScore { get; set; }

        public decimal OtifScore { get; set; }

        public decimal PriceCompetitiveness { get; set; }

        public decimal ComplianceScore { get; set; }

        public decimal LeadTimeAdherence { get; set; }
    }

    public class LeadTimeVarianceBarDto
    {
        public string ItemCode { get; set; } = string.Empty;

        public string VendorName { get; set; } = string.Empty;

        public int ContractualLeadTimeDays { get; set; }

        public int ActualAverageLeadTimeDays { get; set; }

        public int VarianceDays { get; set; }
    }

    // Report DTOs
    public class VendorReportFilterDto
    {
        public string? TenantId { get; set; }

        public int? VendorId { get; set; }

        public string? ItemCategory { get; set; }

        public DateOnly? FromDate { get; set; }

        public DateOnly? ToDate { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 50;
    }

    public class LeadTimeVarianceReportRowDto
    {
        public string VendorCode { get; set; } = string.Empty;

        public string VendorName { get; set; } = string.Empty;

        public string ItemCode { get; set; } = string.Empty;

        public string PoNumber { get; set; } = string.Empty;

        public DateOnly OrderDate { get; set; }

        public DateOnly PromisedDeliveryDate { get; set; }

        public DateOnly ActualGrnDate { get; set; }

        public int VarianceDays { get; set; }

        public bool IsOnTime { get; set; }

        public string DelayRootCause { get; set; } = string.Empty;
    }

    public class CategorySpendConcentrationReportDto
    {
        public int FiscalYear { get; set; }

        public decimal TotalProcurementSpend { get; set; }

        public List<CategoryConcentrationRowDto> Categories { get; set; } = new();
    }

    public class CategoryConcentrationRowDto
    {
        public string CategoryName { get; set; } = string.Empty;

        public decimal TotalCategorySpend { get; set; }

        public decimal HerfindahlHirschmanIndex { get; set; }

        public bool IsHighConcentrationRisk { get; set; }

        public string DominantVendorName { get; set; } = string.Empty;

        public decimal DominantVendorSharePct { get; set; }
    }

    public class MsmePaymentDueAuditRowDto
    {
        public string VendorCode { get; set; } = string.Empty;

        public string VendorName { get; set; } = string.Empty;

        public string MsmeRegistrationNumber { get; set; } = string.Empty;

        public string MsmeCategory { get; set; } = string.Empty;

        public string BillNumber { get; set; } = string.Empty;

        public DateOnly BillDate { get; set; }

        public decimal BillAmount { get; set; }

        public DateOnly StatutoryDueDate { get; set; }

        public int DaysRemaining { get; set; }

        public bool IsOverdue { get; set; }

        public decimal PotentialTaxDisallowanceAmount { get; set; }
    }

    public class QcDefectParetoReportDto
    {
        public int FiscalYear { get; set; }

        public decimal TotalRejectedMaterialValue { get; set; }

        public List<QcDefectParetoRowDto> ParetoItems { get; set; } = new();
    }

    public class QcDefectParetoRowDto
    {
        public string VendorName { get; set; } = string.Empty;

        public string ItemCategory { get; set; } = string.Empty;

        public int TotalLotsReceived { get; set; }

        public int TotalLotsRejected { get; set; }

        public decimal RejectionRatePercentage { get; set; }

        public decimal RejectionValue { get; set; }

        public decimal CumulativePercentage { get; set; }

        public bool IsParetoTop80 { get; set; }
    }

    // Frontend compatibility DTOs for Vendor Reports & Dashboard Facades
    public class VendorReportFilterFrontendRequestDto
    {
        public string? ReportType { get; set; }
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? VendorType { get; set; }
        public string? Tag { get; set; }
    }

    public class VendorReportColumnDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class VendorReportResultDto
    {
        public string ReportType { get; set; } = "master";
        public string Title { get; set; } = "Vendor Master Report";
        public string GeneratedOn { get; set; } = DateTime.UtcNow.ToString("o");
        public List<VendorReportColumnDto> Columns { get; set; } = new();
        public List<Dictionary<string, object?>> Rows { get; set; } = new();
        public int TotalRows { get; set; }
    }

    public class VendorDistributionItemDto
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class VendorDashboardSummaryFrontendDto
    {
        public int TotalVendors { get; set; }
        public int ActiveVendors { get; set; }
        public int BlockedVendors { get; set; }
        public int PendingApproval { get; set; }
        public int PreferredVendors { get; set; }
        public decimal AverageRating { get; set; }
        public List<VendorDistributionItemDto> CountryDistribution { get; set; } = new();
        public List<VendorDistributionItemDto> VendorTypeDistribution { get; set; } = new();
    }

    public class VendorPerformanceMetricsFrontendDto
    {
        public int VendorId { get; set; }
        public string VendorCode { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public decimal OnTimeDeliveryPercent { get; set; } = 96.5m;
        public decimal PurchaseValue { get; set; } = 0m;
        public int OrderCount { get; set; } = 5;
        public decimal GrnSuccessPercent { get; set; } = 98.2m;
        public decimal InvoiceAccuracyPercent { get; set; } = 99.0m;
        public decimal QualityRating { get; set; } = 4.8m;
        public decimal AverageRating { get; set; } = 4.8m;
        public decimal ResponseTimeHours { get; set; } = 4.0m;
        public decimal VendorScore { get; set; } = 95.0m;
        public decimal PreferredVendorScore { get; set; } = 90.0m;
        public string Currency { get; set; } = "INR";
    }

    public class VendorPerformanceDashboardFrontendDto
    {
        public string GeneratedOn { get; set; } = DateTime.UtcNow.ToString("o");
        public decimal AverageOnTimeDelivery { get; set; } = 96.5m;
        public decimal TotalPurchaseValue { get; set; } = 0m;
        public int TotalOrders { get; set; } = 0;
        public decimal AverageGrnSuccess { get; set; } = 98.2m;
        public decimal AverageInvoiceAccuracy { get; set; } = 99.0m;
        public decimal AverageQualityRating { get; set; } = 4.8m;
        public decimal AverageRating { get; set; } = 4.8m;
        public decimal AverageResponseTimeHours { get; set; } = 4.0m;
        public decimal AverageVendorScore { get; set; } = 95.0m;
        public decimal AveragePreferredScore { get; set; } = 90.0m;
        public List<VendorPerformanceMetricsFrontendDto> TopVendors { get; set; } = new();
        public List<VendorPerformanceMetricsFrontendDto> Rows { get; set; } = new();
    }
}
