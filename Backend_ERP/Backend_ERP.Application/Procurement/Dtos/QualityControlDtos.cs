using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement.Dtos
{
    public class QcAttachmentDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("sizeKb")]
        public double SizeKb { get; set; }

        [JsonPropertyName("uploadedBy")]
        public string UploadedBy { get; set; } = string.Empty;

        [JsonPropertyName("uploadedAt")]
        public string UploadedAt { get; set; } = string.Empty;

        [JsonPropertyName("kind")]
        public string? Kind { get; set; }
    }

    public class QcTimelineEventDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("user")]
        public string User { get; set; } = string.Empty;

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("fromStatus")]
        public string? FromStatus { get; set; }

        [JsonPropertyName("toStatus")]
        public string? ToStatus { get; set; }

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }
    }

    public class QcChecklistItemDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("parameter")]
        public string Parameter { get; set; } = string.Empty;

        [JsonPropertyName("specification")]
        public string Specification { get; set; } = string.Empty;

        [JsonPropertyName("actualValue")]
        public string ActualValue { get; set; } = string.Empty;

        [JsonPropertyName("result")]
        public QcCheckResult Result { get; set; }

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }
    }

    public class ListQueryDto
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? Source { get; set; }
        public int? ProductId { get; set; }
        public int? MaterialId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? Inspector { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class StatusActionRequestDto
    {
        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }

        [JsonPropertyName("approvedBy")]
        public string? ApprovedBy { get; set; }
    }

    // ── Incoming Inspection ──

    public class IncomingListItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("inspectionNumber")]
        public string InspectionNumber { get; set; } = string.Empty;

        [JsonPropertyName("inspectionDate")]
        public DateTime InspectionDate { get; set; }

        [JsonPropertyName("supplierName")]
        public string SupplierName { get; set; } = string.Empty;

        [JsonPropertyName("vendorName")]
        public string VendorName { get; set; } = string.Empty;

        [JsonPropertyName("purchaseOrderNumber")]
        public string PurchaseOrderNumber { get; set; } = string.Empty;

        [JsonPropertyName("grnNumber")]
        public string GRNNumber { get; set; } = string.Empty;

        [JsonPropertyName("materialCode")]
        public string MaterialCode { get; set; } = string.Empty;

        [JsonPropertyName("materialName")]
        public string MaterialName { get; set; } = string.Empty;

        [JsonPropertyName("batchNumber")]
        public string BatchNumber { get; set; } = string.Empty;

        [JsonPropertyName("warehouseName")]
        public string WarehouseName { get; set; } = string.Empty;

        [JsonPropertyName("inspector")]
        public string Inspector { get; set; } = string.Empty;

        [JsonPropertyName("acceptedQuantity")]
        public decimal AcceptedQuantity { get; set; }

        [JsonPropertyName("rejectedQuantity")]
        public decimal RejectedQuantity { get; set; }

        [JsonPropertyName("inspectionResult")]
        public InspectionResult InspectionResult { get; set; }

        [JsonPropertyName("status")]
        public IncomingInspectionStatus Status { get; set; }
    }

    public class IncomingDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("inspectionNumber")]
        public string InspectionNumber { get; set; } = string.Empty;

        [JsonPropertyName("inspectionDate")]
        public DateTime InspectionDate { get; set; }

        [JsonPropertyName("supplierId")]
        public int SupplierId { get; set; }

        [JsonPropertyName("supplierName")]
        public string SupplierName { get; set; } = string.Empty;

        [JsonPropertyName("vendorId")]
        public int VendorId { get; set; }

        [JsonPropertyName("vendorName")]
        public string VendorName { get; set; } = string.Empty;

        [JsonPropertyName("purchaseOrderId")]
        public int PurchaseOrderId { get; set; }

        [JsonPropertyName("purchaseOrderNumber")]
        public string PurchaseOrderNumber { get; set; } = string.Empty;

        [JsonPropertyName("grnId")]
        public int GRNId { get; set; }

        [JsonPropertyName("grnNumber")]
        public string GRNNumber { get; set; } = string.Empty;

        [JsonPropertyName("materialId")]
        public int MaterialId { get; set; }

        [JsonPropertyName("materialCode")]
        public string MaterialCode { get; set; } = string.Empty;

        [JsonPropertyName("materialName")]
        public string MaterialName { get; set; } = string.Empty;

        [JsonPropertyName("batchNumber")]
        public string BatchNumber { get; set; } = string.Empty;

        [JsonPropertyName("warehouseId")]
        public int WarehouseId { get; set; }

        [JsonPropertyName("warehouseName")]
        public string WarehouseName { get; set; } = string.Empty;

        [JsonPropertyName("inspector")]
        public string Inspector { get; set; } = string.Empty;

        [JsonPropertyName("inspectionType")]
        public InspectionType InspectionType { get; set; }

        [JsonPropertyName("inspectionMethod")]
        public InspectionMethod InspectionMethod { get; set; }

        [JsonPropertyName("samplingQuantity")]
        public decimal SamplingQuantity { get; set; }

        [JsonPropertyName("acceptedQuantity")]
        public decimal AcceptedQuantity { get; set; }

        [JsonPropertyName("rejectedQuantity")]
        public decimal RejectedQuantity { get; set; }

        [JsonPropertyName("pendingQuantity")]
        public decimal PendingQuantity { get; set; }

        [JsonPropertyName("inspectionResult")]
        public InspectionResult InspectionResult { get; set; }

        [JsonPropertyName("checklist")]
        public List<QcChecklistItemDto> Checklist { get; set; } = new();

        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("attachments")]
        public List<QcAttachmentDto> Attachments { get; set; } = new();

        [JsonPropertyName("images")]
        public List<QcAttachmentDto> Images { get; set; } = new();

        [JsonPropertyName("timeline")]
        public List<QcTimelineEventDto> Timeline { get; set; } = new();

        [JsonPropertyName("status")]
        public IncomingInspectionStatus Status { get; set; }

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedBy")]
        public string UpdatedBy { get; set; } = string.Empty;

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }

    public class IncomingCreateRequestDto
    {
        [JsonPropertyName("inspectionDate")]
        public DateTime InspectionDate { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("supplierId")]
        public int SupplierId { get; set; }

        [JsonPropertyName("supplierName")]
        public string SupplierName { get; set; } = string.Empty;

        [JsonPropertyName("vendorId")]
        public int VendorId { get; set; }

        [JsonPropertyName("vendorName")]
        public string VendorName { get; set; } = string.Empty;

        [JsonPropertyName("purchaseOrderId")]
        public int PurchaseOrderId { get; set; }

        [JsonPropertyName("purchaseOrderNumber")]
        public string PurchaseOrderNumber { get; set; } = string.Empty;

        [JsonPropertyName("grnId")]
        public int GRNId { get; set; }

        [JsonPropertyName("grnNumber")]
        public string GRNNumber { get; set; } = string.Empty;

        [JsonPropertyName("materialId")]
        public int MaterialId { get; set; }

        [JsonPropertyName("materialCode")]
        public string MaterialCode { get; set; } = string.Empty;

        [JsonPropertyName("materialName")]
        public string MaterialName { get; set; } = string.Empty;

        [JsonPropertyName("batchNumber")]
        public string BatchNumber { get; set; } = string.Empty;

        [JsonPropertyName("warehouseId")]
        public int WarehouseId { get; set; }

        [JsonPropertyName("warehouseName")]
        public string WarehouseName { get; set; } = string.Empty;

        [JsonPropertyName("inspector")]
        public string Inspector { get; set; } = string.Empty;

        [JsonPropertyName("inspectionType")]
        public InspectionType InspectionType { get; set; } = InspectionType.Visual;

        [JsonPropertyName("inspectionMethod")]
        public InspectionMethod InspectionMethod { get; set; } = InspectionMethod.Sampling;

        [JsonPropertyName("samplingQuantity")]
        public decimal SamplingQuantity { get; set; }

        [JsonPropertyName("acceptedQuantity")]
        public decimal AcceptedQuantity { get; set; }

        [JsonPropertyName("rejectedQuantity")]
        public decimal RejectedQuantity { get; set; }

        [JsonPropertyName("pendingQuantity")]
        public decimal PendingQuantity { get; set; }

        [JsonPropertyName("checklist")]
        public List<QcChecklistItemDto>? Checklist { get; set; }

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    public class IncomingDashboardDto
    {
        [JsonPropertyName("pendingInspection")]
        public int PendingInspection { get; set; }

        [JsonPropertyName("approved")]
        public int Approved { get; set; }

        [JsonPropertyName("rejected")]
        public int Rejected { get; set; }

        [JsonPropertyName("todaysInspection")]
        public int TodaysInspection { get; set; }
    }

    // ── In-Process QC ──

    public class InProcessListItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("qcNumber")]
        public string QCNumber { get; set; } = string.Empty;

        [JsonPropertyName("productionEntryNumber")]
        public string ProductionEntryNumber { get; set; } = string.Empty;

        [JsonPropertyName("workOrderNumber")]
        public string WorkOrderNumber { get; set; } = string.Empty;

        [JsonPropertyName("machineName")]
        public string MachineName { get; set; } = string.Empty;

        [JsonPropertyName("operator")]
        public string Operator { get; set; } = string.Empty;

        [JsonPropertyName("shift")]
        public Shift Shift { get; set; }

        [JsonPropertyName("stage")]
        public string Stage { get; set; } = string.Empty;

        [JsonPropertyName("productCode")]
        public string ProductCode { get; set; } = string.Empty;

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("batchNumber")]
        public string BatchNumber { get; set; } = string.Empty;

        [JsonPropertyName("parameter")]
        public string Parameter { get; set; } = string.Empty;

        [JsonPropertyName("result")]
        public QcCheckResult Result { get; set; }

        [JsonPropertyName("status")]
        public InProcessQcStatus Status { get; set; }
    }

    public class InProcessDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("qcNumber")]
        public string QCNumber { get; set; } = string.Empty;

        [JsonPropertyName("productionEntryId")]
        public int ProductionEntryId { get; set; }

        [JsonPropertyName("productionEntryNumber")]
        public string ProductionEntryNumber { get; set; } = string.Empty;

        [JsonPropertyName("workOrderId")]
        public int WorkOrderId { get; set; }

        [JsonPropertyName("workOrderNumber")]
        public string WorkOrderNumber { get; set; } = string.Empty;

        [JsonPropertyName("machineId")]
        public int MachineId { get; set; }

        [JsonPropertyName("machineCode")]
        public string MachineCode { get; set; } = string.Empty;

        [JsonPropertyName("machineName")]
        public string MachineName { get; set; } = string.Empty;

        [JsonPropertyName("operator")]
        public string Operator { get; set; } = string.Empty;

        [JsonPropertyName("shift")]
        public Shift Shift { get; set; }

        [JsonPropertyName("stage")]
        public string Stage { get; set; } = string.Empty;

        [JsonPropertyName("productId")]
        public int ProductId { get; set; }

        [JsonPropertyName("productCode")]
        public string ProductCode { get; set; } = string.Empty;

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("batchNumber")]
        public string BatchNumber { get; set; } = string.Empty;

        [JsonPropertyName("parameter")]
        public string Parameter { get; set; } = string.Empty;

        [JsonPropertyName("tolerance")]
        public string Tolerance { get; set; } = string.Empty;

        [JsonPropertyName("expectedValue")]
        public string ExpectedValue { get; set; } = string.Empty;

        [JsonPropertyName("actualValue")]
        public string ActualValue { get; set; } = string.Empty;

        [JsonPropertyName("result")]
        public QcCheckResult Result { get; set; }

        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("attachments")]
        public List<QcAttachmentDto> Attachments { get; set; } = new();

        [JsonPropertyName("timeline")]
        public List<QcTimelineEventDto> Timeline { get; set; } = new();

        [JsonPropertyName("status")]
        public InProcessQcStatus Status { get; set; }

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedBy")]
        public string UpdatedBy { get; set; } = string.Empty;

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }

    public class InProcessCreateRequestDto
    {
        [JsonPropertyName("productionEntryId")]
        public int ProductionEntryId { get; set; }

        [JsonPropertyName("productionEntryNumber")]
        public string ProductionEntryNumber { get; set; } = string.Empty;

        [JsonPropertyName("workOrderId")]
        public int WorkOrderId { get; set; }

        [JsonPropertyName("workOrderNumber")]
        public string WorkOrderNumber { get; set; } = string.Empty;

        [JsonPropertyName("machineId")]
        public int MachineId { get; set; }

        [JsonPropertyName("machineCode")]
        public string MachineCode { get; set; } = string.Empty;

        [JsonPropertyName("machineName")]
        public string MachineName { get; set; } = string.Empty;

        [JsonPropertyName("operator")]
        public string Operator { get; set; } = string.Empty;

        [JsonPropertyName("shift")]
        public Shift Shift { get; set; } = Shift.A;

        [JsonPropertyName("stage")]
        public string Stage { get; set; } = string.Empty;

        [JsonPropertyName("productId")]
        public int ProductId { get; set; }

        [JsonPropertyName("productCode")]
        public string ProductCode { get; set; } = string.Empty;

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("batchNumber")]
        public string BatchNumber { get; set; } = string.Empty;

        [JsonPropertyName("parameter")]
        public string Parameter { get; set; } = string.Empty;

        [JsonPropertyName("tolerance")]
        public string Tolerance { get; set; } = string.Empty;

        [JsonPropertyName("expectedValue")]
        public string ExpectedValue { get; set; } = string.Empty;

        [JsonPropertyName("actualValue")]
        public string ActualValue { get; set; } = string.Empty;

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    public class InProcessDashboardDto
    {
        [JsonPropertyName("runningChecks")]
        public int RunningChecks { get; set; }

        [JsonPropertyName("passed")]
        public int Passed { get; set; }

        [JsonPropertyName("failed")]
        public int Failed { get; set; }

        [JsonPropertyName("pending")]
        public int Pending { get; set; }
    }

    // ── Final Inspection ──

    public class FinalInspectionParameterDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("expected")]
        public string Expected { get; set; } = string.Empty;

        [JsonPropertyName("actual")]
        public string Actual { get; set; } = string.Empty;

        [JsonPropertyName("result")]
        public QcCheckResult Result { get; set; }
    }

    public class FinalListItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("inspectionNumber")]
        public string InspectionNumber { get; set; } = string.Empty;

        [JsonPropertyName("inspectionDate")]
        public DateTime InspectionDate { get; set; }

        [JsonPropertyName("finishedProductCode")]
        public string FinishedProductCode { get; set; } = string.Empty;

        [JsonPropertyName("finishedProductName")]
        public string FinishedProductName { get; set; } = string.Empty;

        [JsonPropertyName("productionEntryNumber")]
        public string ProductionEntryNumber { get; set; } = string.Empty;

        [JsonPropertyName("productionBatch")]
        public string ProductionBatch { get; set; } = string.Empty;

        [JsonPropertyName("inspector")]
        public string Inspector { get; set; } = string.Empty;

        [JsonPropertyName("acceptedQuantity")]
        public decimal AcceptedQuantity { get; set; }

        [JsonPropertyName("rejectedQuantity")]
        public decimal RejectedQuantity { get; set; }

        [JsonPropertyName("status")]
        public FinalInspectionStatus Status { get; set; }
    }

    public class FinalDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("inspectionNumber")]
        public string InspectionNumber { get; set; } = string.Empty;

        [JsonPropertyName("inspectionDate")]
        public DateTime InspectionDate { get; set; }

        [JsonPropertyName("finishedProductId")]
        public int FinishedProductId { get; set; }

        [JsonPropertyName("finishedProductCode")]
        public string FinishedProductCode { get; set; } = string.Empty;

        [JsonPropertyName("finishedProductName")]
        public string FinishedProductName { get; set; } = string.Empty;

        [JsonPropertyName("productionEntryId")]
        public int ProductionEntryId { get; set; }

        [JsonPropertyName("productionEntryNumber")]
        public string ProductionEntryNumber { get; set; } = string.Empty;

        [JsonPropertyName("productionBatch")]
        public string ProductionBatch { get; set; } = string.Empty;

        [JsonPropertyName("inspector")]
        public string Inspector { get; set; } = string.Empty;

        [JsonPropertyName("dimension")]
        public string Dimension { get; set; } = string.Empty;

        [JsonPropertyName("weight")]
        public string Weight { get; set; } = string.Empty;

        [JsonPropertyName("strength")]
        public string Strength { get; set; } = string.Empty;

        [JsonPropertyName("surfaceFinish")]
        public string SurfaceFinish { get; set; } = string.Empty;

        [JsonPropertyName("visualCheck")]
        public string VisualCheck { get; set; } = string.Empty;

        [JsonPropertyName("parameters")]
        public List<FinalInspectionParameterDto> Parameters { get; set; } = new();

        [JsonPropertyName("acceptedQuantity")]
        public decimal AcceptedQuantity { get; set; }

        [JsonPropertyName("rejectedQuantity")]
        public decimal RejectedQuantity { get; set; }

        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("attachments")]
        public List<QcAttachmentDto> Attachments { get; set; } = new();

        [JsonPropertyName("timeline")]
        public List<QcTimelineEventDto> Timeline { get; set; } = new();

        [JsonPropertyName("status")]
        public FinalInspectionStatus Status { get; set; }

        [JsonPropertyName("testCertificateId")]
        public int? TestCertificateId { get; set; }

        [JsonPropertyName("loadTestId")]
        public int? LoadTestId { get; set; }

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedBy")]
        public string UpdatedBy { get; set; } = string.Empty;

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }

    public class FinalCreateRequestDto
    {
        [JsonPropertyName("inspectionDate")]
        public DateTime InspectionDate { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("finishedProductId")]
        public int FinishedProductId { get; set; }

        [JsonPropertyName("finishedProductCode")]
        public string FinishedProductCode { get; set; } = string.Empty;

        [JsonPropertyName("finishedProductName")]
        public string FinishedProductName { get; set; } = string.Empty;

        [JsonPropertyName("productionEntryId")]
        public int ProductionEntryId { get; set; }

        [JsonPropertyName("productionEntryNumber")]
        public string ProductionEntryNumber { get; set; } = string.Empty;

        [JsonPropertyName("productionBatch")]
        public string ProductionBatch { get; set; } = string.Empty;

        [JsonPropertyName("inspector")]
        public string Inspector { get; set; } = string.Empty;

        [JsonPropertyName("dimension")]
        public string Dimension { get; set; } = string.Empty;

        [JsonPropertyName("weight")]
        public string Weight { get; set; } = string.Empty;

        [JsonPropertyName("strength")]
        public string Strength { get; set; } = string.Empty;

        [JsonPropertyName("surfaceFinish")]
        public string SurfaceFinish { get; set; } = string.Empty;

        [JsonPropertyName("visualCheck")]
        public string VisualCheck { get; set; } = string.Empty;

        [JsonPropertyName("parameters")]
        public List<FinalInspectionParameterDto>? Parameters { get; set; }

        [JsonPropertyName("acceptedQuantity")]
        public decimal AcceptedQuantity { get; set; }

        [JsonPropertyName("rejectedQuantity")]
        public decimal RejectedQuantity { get; set; }

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    public class FinalDashboardDto
    {
        [JsonPropertyName("todaysInspection")]
        public int TodaysInspection { get; set; }

        [JsonPropertyName("approved")]
        public int Approved { get; set; }

        [JsonPropertyName("rejected")]
        public int Rejected { get; set; }

        [JsonPropertyName("pending")]
        public int Pending { get; set; }
    }

    // ── Load Test ──

    public class LoadTestListItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("reportNumber")]
        public string ReportNumber { get; set; } = string.Empty;

        [JsonPropertyName("testDate")]
        public DateTime TestDate { get; set; }

        [JsonPropertyName("productCode")]
        public string ProductCode { get; set; } = string.Empty;

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("finalInspectionNumber")]
        public string FinalInspectionNumber { get; set; } = string.Empty;

        [JsonPropertyName("machineName")]
        public string MachineName { get; set; } = string.Empty;

        [JsonPropertyName("loadCapacity")]
        public decimal LoadCapacity { get; set; }

        [JsonPropertyName("appliedLoad")]
        public decimal AppliedLoad { get; set; }

        [JsonPropertyName("result")]
        public LoadTestStatus Result { get; set; }

        [JsonPropertyName("passFail")]
        public string PassFail { get; set; } = "Pending";
    }

    public class LoadTestDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("reportNumber")]
        public string ReportNumber { get; set; } = string.Empty;

        [JsonPropertyName("testDate")]
        public DateTime TestDate { get; set; }

        [JsonPropertyName("productId")]
        public int ProductId { get; set; }

        [JsonPropertyName("productCode")]
        public string ProductCode { get; set; } = string.Empty;

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("finalInspectionId")]
        public int FinalInspectionId { get; set; }

        [JsonPropertyName("finalInspectionNumber")]
        public string FinalInspectionNumber { get; set; } = string.Empty;

        [JsonPropertyName("machineId")]
        public int MachineId { get; set; }

        [JsonPropertyName("machineCode")]
        public string MachineCode { get; set; } = string.Empty;

        [JsonPropertyName("machineName")]
        public string MachineName { get; set; } = string.Empty;

        [JsonPropertyName("loadCapacity")]
        public decimal LoadCapacity { get; set; }

        [JsonPropertyName("appliedLoad")]
        public decimal AppliedLoad { get; set; }

        [JsonPropertyName("durationMinutes")]
        public int DurationMinutes { get; set; }

        [JsonPropertyName("result")]
        public LoadTestStatus Result { get; set; }

        [JsonPropertyName("passFail")]
        public string PassFail { get; set; } = "Pending";

        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("attachments")]
        public List<QcAttachmentDto> Attachments { get; set; } = new();

        [JsonPropertyName("images")]
        public List<QcAttachmentDto> Images { get; set; } = new();

        [JsonPropertyName("timeline")]
        public List<QcTimelineEventDto> Timeline { get; set; } = new();

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedBy")]
        public string UpdatedBy { get; set; } = string.Empty;

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }

    public class LoadTestCreateRequestDto
    {
        [JsonPropertyName("testDate")]
        public DateTime TestDate { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("productId")]
        public int ProductId { get; set; }

        [JsonPropertyName("productCode")]
        public string ProductCode { get; set; } = string.Empty;

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("finalInspectionId")]
        public int FinalInspectionId { get; set; }

        [JsonPropertyName("finalInspectionNumber")]
        public string FinalInspectionNumber { get; set; } = string.Empty;

        [JsonPropertyName("machineId")]
        public int MachineId { get; set; }

        [JsonPropertyName("machineCode")]
        public string MachineCode { get; set; } = string.Empty;

        [JsonPropertyName("machineName")]
        public string MachineName { get; set; } = string.Empty;

        [JsonPropertyName("loadCapacity")]
        public decimal LoadCapacity { get; set; }

        [JsonPropertyName("appliedLoad")]
        public decimal AppliedLoad { get; set; }

        [JsonPropertyName("durationMinutes")]
        public int DurationMinutes { get; set; }

        [JsonPropertyName("result")]
        public LoadTestStatus Result { get; set; } = LoadTestStatus.Pending;

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    public class LoadTestDashboardDto
    {
        [JsonPropertyName("totalTests")]
        public int TotalTests { get; set; }

        [JsonPropertyName("passed")]
        public int Passed { get; set; }

        [JsonPropertyName("failed")]
        public int Failed { get; set; }

        [JsonPropertyName("pending")]
        public int Pending { get; set; }
    }

    // ── Test Certificate ──

    public class CertificateListItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("certificateNumber")]
        public string CertificateNumber { get; set; } = string.Empty;

        [JsonPropertyName("certificateDate")]
        public DateTime CertificateDate { get; set; }

        [JsonPropertyName("customerName")]
        public string CustomerName { get; set; } = string.Empty;

        [JsonPropertyName("productCode")]
        public string ProductCode { get; set; } = string.Empty;

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("finalInspectionNumber")]
        public string FinalInspectionNumber { get; set; } = string.Empty;

        [JsonPropertyName("batchNumber")]
        public string BatchNumber { get; set; } = string.Empty;

        [JsonPropertyName("issuedBy")]
        public string IssuedBy { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public CertificateStatus Status { get; set; }
    }

    public class CertificateDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("certificateNumber")]
        public string CertificateNumber { get; set; } = string.Empty;

        [JsonPropertyName("certificateDate")]
        public DateTime CertificateDate { get; set; }

        [JsonPropertyName("customerId")]
        public int CustomerId { get; set; }

        [JsonPropertyName("customerName")]
        public string CustomerName { get; set; } = string.Empty;

        [JsonPropertyName("productId")]
        public int ProductId { get; set; }

        [JsonPropertyName("productCode")]
        public string ProductCode { get; set; } = string.Empty;

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("finalInspectionId")]
        public int FinalInspectionId { get; set; }

        [JsonPropertyName("finalInspectionNumber")]
        public string FinalInspectionNumber { get; set; } = string.Empty;

        [JsonPropertyName("loadTestId")]
        public int? LoadTestId { get; set; }

        [JsonPropertyName("loadTestNumber")]
        public string? LoadTestNumber { get; set; }

        [JsonPropertyName("batchNumber")]
        public string BatchNumber { get; set; } = string.Empty;

        [JsonPropertyName("issuedBy")]
        public string IssuedBy { get; set; } = string.Empty;

        [JsonPropertyName("approvedBy")]
        public string? ApprovedBy { get; set; }

        [JsonPropertyName("status")]
        public CertificateStatus Status { get; set; }

        [JsonPropertyName("expiryDate")]
        public DateTime? ExpiryDate { get; set; }

        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("attachments")]
        public List<QcAttachmentDto> Attachments { get; set; } = new();

        [JsonPropertyName("timeline")]
        public List<QcTimelineEventDto> Timeline { get; set; } = new();

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedBy")]
        public string UpdatedBy { get; set; } = string.Empty;

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }

    public class CertificateCreateRequestDto
    {
        [JsonPropertyName("certificateDate")]
        public DateTime CertificateDate { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("customerId")]
        public int CustomerId { get; set; }

        [JsonPropertyName("customerName")]
        public string CustomerName { get; set; } = string.Empty;

        [JsonPropertyName("productId")]
        public int ProductId { get; set; }

        [JsonPropertyName("productCode")]
        public string ProductCode { get; set; } = string.Empty;

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("finalInspectionId")]
        public int FinalInspectionId { get; set; }

        [JsonPropertyName("finalInspectionNumber")]
        public string FinalInspectionNumber { get; set; } = string.Empty;

        [JsonPropertyName("loadTestId")]
        public int? LoadTestId { get; set; }

        [JsonPropertyName("loadTestNumber")]
        public string? LoadTestNumber { get; set; }

        [JsonPropertyName("batchNumber")]
        public string BatchNumber { get; set; } = string.Empty;

        [JsonPropertyName("issuedBy")]
        public string IssuedBy { get; set; } = string.Empty;

        [JsonPropertyName("expiryDate")]
        public DateTime? ExpiryDate { get; set; }

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    public class CertificateDashboardDto
    {
        [JsonPropertyName("issuedCertificates")]
        public int IssuedCertificates { get; set; }

        [JsonPropertyName("pendingApproval")]
        public int PendingApproval { get; set; }

        [JsonPropertyName("expired")]
        public int Expired { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }

    // ── Rejection Analysis ──

    public class RejectionListItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("rejectionNumber")]
        public string RejectionNumber { get; set; } = string.Empty;

        [JsonPropertyName("rejectionDate")]
        public DateTime RejectionDate { get; set; }

        [JsonPropertyName("source")]
        public RejectionSource Source { get; set; }

        [JsonPropertyName("sourceRecordNumber")]
        public string SourceRecordNumber { get; set; } = string.Empty;

        [JsonPropertyName("materialName")]
        public string? MaterialName { get; set; }

        [JsonPropertyName("productName")]
        public string? ProductName { get; set; }

        [JsonPropertyName("batchNumber")]
        public string BatchNumber { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;

        [JsonPropertyName("department")]
        public string Department { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public RejectionAnalysisStatus Status { get; set; }
    }

    public class RejectionDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("rejectionNumber")]
        public string RejectionNumber { get; set; } = string.Empty;

        [JsonPropertyName("rejectionDate")]
        public DateTime RejectionDate { get; set; }

        [JsonPropertyName("source")]
        public RejectionSource Source { get; set; }

        [JsonPropertyName("sourceRecordId")]
        public int SourceRecordId { get; set; }

        [JsonPropertyName("sourceRecordNumber")]
        public string SourceRecordNumber { get; set; } = string.Empty;

        [JsonPropertyName("materialId")]
        public int? MaterialId { get; set; }

        [JsonPropertyName("materialCode")]
        public string? MaterialCode { get; set; }

        [JsonPropertyName("materialName")]
        public string? MaterialName { get; set; }

        [JsonPropertyName("productId")]
        public int? ProductId { get; set; }

        [JsonPropertyName("productCode")]
        public string? ProductCode { get; set; }

        [JsonPropertyName("productName")]
        public string? ProductName { get; set; }

        [JsonPropertyName("batchNumber")]
        public string BatchNumber { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;

        [JsonPropertyName("rootCause")]
        public string RootCause { get; set; } = string.Empty;

        [JsonPropertyName("department")]
        public string Department { get; set; } = string.Empty;

        [JsonPropertyName("operator")]
        public string Operator { get; set; } = string.Empty;

        [JsonPropertyName("machineId")]
        public int? MachineId { get; set; }

        [JsonPropertyName("machineCode")]
        public string? MachineCode { get; set; }

        [JsonPropertyName("machineName")]
        public string? MachineName { get; set; }

        [JsonPropertyName("supplierName")]
        public string? SupplierName { get; set; }

        [JsonPropertyName("correctiveAction")]
        public string CorrectiveAction { get; set; } = string.Empty;

        [JsonPropertyName("preventiveAction")]
        public string PreventiveAction { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public RejectionAnalysisStatus Status { get; set; }

        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("attachments")]
        public List<QcAttachmentDto> Attachments { get; set; } = new();

        [JsonPropertyName("timeline")]
        public List<QcTimelineEventDto> Timeline { get; set; } = new();

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedBy")]
        public string UpdatedBy { get; set; } = string.Empty;

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }

    public class RejectionCreateRequestDto
    {
        [JsonPropertyName("source")]
        public RejectionSource Source { get; set; } = RejectionSource.Incoming;

        [JsonPropertyName("sourceRecordId")]
        public int SourceRecordId { get; set; }

        [JsonPropertyName("sourceRecordNumber")]
        public string SourceRecordNumber { get; set; } = string.Empty;

        [JsonPropertyName("materialId")]
        public int? MaterialId { get; set; }

        [JsonPropertyName("materialCode")]
        public string? MaterialCode { get; set; }

        [JsonPropertyName("materialName")]
        public string? MaterialName { get; set; }

        [JsonPropertyName("productId")]
        public int? ProductId { get; set; }

        [JsonPropertyName("productCode")]
        public string? ProductCode { get; set; }

        [JsonPropertyName("productName")]
        public string? ProductName { get; set; }

        [JsonPropertyName("batchNumber")]
        public string BatchNumber { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;

        [JsonPropertyName("rootCause")]
        public string? RootCause { get; set; }

        [JsonPropertyName("department")]
        public string Department { get; set; } = string.Empty;

        [JsonPropertyName("operator")]
        public string? Operator { get; set; }

        [JsonPropertyName("machineId")]
        public int? MachineId { get; set; }

        [JsonPropertyName("machineCode")]
        public string? MachineCode { get; set; }

        [JsonPropertyName("machineName")]
        public string? MachineName { get; set; }

        [JsonPropertyName("supplierName")]
        public string? SupplierName { get; set; }

        [JsonPropertyName("correctiveAction")]
        public string? CorrectiveAction { get; set; }

        [JsonPropertyName("preventiveAction")]
        public string? PreventiveAction { get; set; }

        [JsonPropertyName("remarks")]
        public string? Remarks { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    public class RejectionDashboardDto
    {
        [JsonPropertyName("totalRejections")]
        public int TotalRejections { get; set; }

        [JsonPropertyName("rejectionPercent")]
        public decimal RejectionPercent { get; set; }

        [JsonPropertyName("topReasons")]
        public List<NameCountDto> TopReasons { get; set; } = new();

        [JsonPropertyName("departmentWise")]
        public List<DepartmentCountDto> DepartmentWise { get; set; } = new();

        [JsonPropertyName("supplierWise")]
        public List<SupplierCountDto> SupplierWise { get; set; } = new();

        [JsonPropertyName("machineWise")]
        public List<MachineCountDto> MachineWise { get; set; } = new();
    }

    public class NameCountDto
    {
        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    public class DepartmentCountDto
    {
        [JsonPropertyName("department")]
        public string Department { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    public class SupplierCountDto
    {
        [JsonPropertyName("supplier")]
        public string Supplier { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    public class MachineCountDto
    {
        [JsonPropertyName("machine")]
        public string Machine { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    public class RejectionReportDto
    {
        [JsonPropertyName("generatedAt")]
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("totalRejections")]
        public int TotalRejections { get; set; }

        [JsonPropertyName("bySource")]
        public List<SourceReportItemDto> BySource { get; set; } = new();

        [JsonPropertyName("byReason")]
        public List<NameCountDto> ByReason { get; set; } = new();

        [JsonPropertyName("byDepartment")]
        public List<DepartmentCountDto> ByDepartment { get; set; } = new();

        [JsonPropertyName("openCount")]
        public int OpenCount { get; set; }

        [JsonPropertyName("closedCount")]
        public int ClosedCount { get; set; }
    }

    public class SourceReportItemDto
    {
        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }
    }
}
