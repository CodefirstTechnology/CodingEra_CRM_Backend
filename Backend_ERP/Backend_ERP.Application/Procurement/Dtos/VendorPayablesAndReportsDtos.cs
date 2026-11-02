using System;
using System.Collections.Generic;

namespace ERP.Application.Procurement.Dtos
{
    // ==========================================
    // 1. BILL SETTLEMENT & KNOCK-OFF DTOS
    // ==========================================
    public class SettleBillRequestDto
    {
        public int VendorId { get; set; }
        public int? PaymentVoucherId { get; set; }
        public int? DebitNoteId { get; set; }
        public decimal TotalSettlementAmount { get; set; }
        public DateTime SettlementDate { get; set; } = DateTime.UtcNow;
        public string? AllocationNotes { get; set; }
        public List<SettleBillAllocationItemDto> Allocations { get; set; } = new();
    }

    public class SettleBillAllocationItemDto
    {
        public int PurchaseBillId { get; set; }
        public decimal AllocatedAmount { get; set; }
        public decimal PaymentExchangeRate { get; set; } = 1.0m;
    }

    public class FifoKnockOffRequestDto
    {
        public int VendorId { get; set; }
        public int PaymentVoucherId { get; set; }
        public decimal AvailablePaymentAmount { get; set; }
        public decimal PaymentExchangeRate { get; set; } = 1.0m;
        public string? AllocationNotes { get; set; }
    }

    public class VendorBillSettlementResultDto
    {
        public bool Success { get; set; }
        public int VendorId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public decimal TotalSettledAmount { get; set; }
        public decimal TotalRealizedFxGainLoss { get; set; }
        public int BillsSettledCount { get; set; }
        public List<SettlementAuditEntryDto> ClearedBills { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    public class SettlementAuditEntryDto
    {
        public int SettlementId { get; set; }
        public int PurchaseBillId { get; set; }
        public string BillNumber { get; set; } = string.Empty;
        public decimal AllocatedAmount { get; set; }
        public decimal OutstandingBefore { get; set; }
        public decimal OutstandingAfter { get; set; }
        public string NewBillStatus { get; set; } = string.Empty;
        public decimal RealizedFxGainLoss { get; set; }
    }

    // ==========================================
    // 2. ACCOUNTS PAYABLE AGING DTOS
    // ==========================================
    public class AccountsPayableAgingGridDto
    {
        public DateTime AsOfDate { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal TotalNotDue { get; set; }
        public decimal Total1To15Days { get; set; }
        public decimal Total16To30Days { get; set; }
        public decimal Total31To45Days { get; set; }
        public decimal TotalOver45Days { get; set; }
        public decimal TotalMsmeRiskAmount { get; set; }
        public List<VendorAgingBucketRowDto> VendorRows { get; set; } = new();
    }

    public class VendorAgingBucketRowDto
    {
        public int VendorId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string MsmeCategory { get; set; } = "None"; // Micro, Small, Medium, Large, None
        public bool IsMsme43BApplicable { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal NotDueAmount { get; set; }
        public decimal Days1To15 { get; set; }
        public decimal Days16To30 { get; set; }
        public decimal Days31To45 { get; set; }
        public decimal DaysOver45 { get; set; }
        public bool HasOverdueBreach { get; set; }
        public decimal ProjectedPenalInterest { get; set; }
    }

    public class MsmeComplianceAlertDto
    {
        public int PurchaseBillId { get; set; }
        public string BillNumber { get; set; } = string.Empty;
        public int VendorId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string MsmeCategory { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public DateTime MsmeDueDate { get; set; }
        public int AllowedDays { get; set; }
        public int OverdueDays { get; set; }
        public decimal OutstandingAmount { get; set; }
        public decimal ProjectedInterestAt3xRepoRate { get; set; }
        public string StatutoryRiskLevel { get; set; } = "Warning"; // Warning, CriticalDisallowance
    }

    // ==========================================
    // 3. WORKFLOW SLA & VELOCITY DTOS
    // ==========================================
    public class WorkflowVelocityReportDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalDocumentsTracked { get; set; }
        public int TotalSlaBreaches { get; set; }
        public decimal OverallSlaComplianceRate { get; set; } // Percentage e.g. 94.5%
        public List<WorkflowStageVelocityDto> StageMetrics { get; set; } = new();
    }

    public class WorkflowStageVelocityDto
    {
        public int StageSequence { get; set; }
        public string StageName { get; set; } = string.Empty;
        public int TotalInstances { get; set; }
        public decimal TargetSlaHours { get; set; }
        public decimal AverageElapsedHours { get; set; }
        public decimal MaxElapsedHours { get; set; }
        public int BreachedCount { get; set; }
        public decimal SlaCompliancePercentage { get; set; }
    }

    public class DepartmentBottleneckScorecardDto
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int TotalTasksAssigned { get; set; }
        public int BreachedTasksCount { get; set; }
        public decimal BreachRatePercentage { get; set; }
        public decimal AverageTurnaroundHours { get; set; }
        public string PrimaryBottleneckStage { get; set; } = string.Empty;
    }

    // ==========================================
    // 4. SPEND ANALYTICS & PPV DTOS
    // ==========================================
    public class PurchasePriceVarianceReportDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalActualSpend { get; set; }
        public decimal TotalStandardSpend { get; set; }
        public decimal NetPpvAmount { get; set; } // Positive = Unfavorable, Negative = Favorable
        public decimal NetPpvPercentage { get; set; }
        public int TotalLineItemsEvaluated { get; set; }
        public List<PpvItemRecordDto> Items { get; set; } = new();
    }

    public class PpvItemRecordDto
    {
        public int RawMaterialId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string PurchaseOrderNumber { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public decimal ReceivedQuantity { get; set; }
        public decimal ActualUnitPrice { get; set; }
        public decimal StandardUnitPrice { get; set; }
        public decimal UnitVariance { get; set; }
        public decimal TotalPpv { get; set; }
        public string VarianceType { get; set; } = "Neutral"; // Favorable, Unfavorable, Neutral
    }

    public class SpendParetoMatrixDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalSpend { get; set; }
        public SpendParetoTierDto TierA { get; set; } = new(); // Top 80%
        public SpendParetoTierDto TierB { get; set; } = new(); // Next 15%
        public SpendParetoTierDto TierC { get; set; } = new(); // Long-tail 5%
    }

    public class SpendParetoTierDto
    {
        public string TierName { get; set; } = string.Empty;
        public decimal TotalSpend { get; set; }
        public decimal SpendPercentage { get; set; }
        public int VendorCount { get; set; }
        public decimal VendorPercentage { get; set; }
        public List<string> TopVendors { get; set; } = new();
    }

    public class SupplierOtifScorecardDto
    {
        public int TotalDeliveries { get; set; }
        public int OnTimeDeliveries { get; set; }
        public int InFullDeliveries { get; set; }
        public int OtifDeliveries { get; set; } // Delivered BOTH On-Time AND In-Full
        public decimal OverallOtifScore { get; set; }
        public List<SupplierOtifRowDto> Suppliers { get; set; } = new();
    }

    public class SupplierOtifRowDto
    {
        public int VendorId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public decimal OnTimePercentage { get; set; }
        public decimal InFullPercentage { get; set; }
        public decimal OtifScorePercentage { get; set; }
        public string RatingGrade { get; set; } = "A"; // A (>=95%), B (85-94%), C (<85%)
    }

    public class ProcurementKpiDashboardDto
    {
        public decimal TotalActiveSpendMtd { get; set; }
        public decimal TotalActiveSpendYtd { get; set; }
        public decimal NetPpvMtd { get; set; }
        public decimal AverageOtifScore { get; set; }
        public decimal TotalAccountsPayableOutstanding { get; set; }
        public decimal TotalMsmeOverdueRisk { get; set; }
        public int ActiveVendorsCount { get; set; }
        public int PendingGrnQcCount { get; set; }
        public decimal WorkflowSlaCompliancePercentage { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public bool IsCached { get; set; }
    }

    // ==========================================
    // 5. FORENSIC AUDIT TRAIL DTOS
    // ==========================================
    public class ProcurementAuditLogDto
    {
        public long Id { get; set; }
        public long BlockIndex { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string ChangedByUser { get; set; } = string.Empty;
        public string? UserIpAddress { get; set; }
        public string? OldValuesJson { get; set; }
        public string? NewValuesJson { get; set; }
        public string PreviousLogHash { get; set; } = string.Empty;
        public string CurrentLogHash { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class AuditChainVerificationResultDto
    {
        public bool IsChainValid { get; set; }
        public long TotalBlocksVerified { get; set; }
        public long? TamperedBlockIndex { get; set; }
        public string? TamperedEntityName { get; set; }
        public string? TamperedEntityId { get; set; }
        public string ExpectedHash { get; set; } = string.Empty;
        public string ActualHash { get; set; } = string.Empty;
        public string VerificationSummary { get; set; } = string.Empty;
        public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
    }
}

