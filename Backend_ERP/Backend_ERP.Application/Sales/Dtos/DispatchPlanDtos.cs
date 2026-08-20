using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ERP.Domain.Sales;

namespace ERP.Application.Sales.Dtos
{
    public class ListQueryDto
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public int? CustomerId { get; set; }
        public int? DispatchId { get; set; }
        public string? DateFrom { get; set; }
        public string? DateTo { get; set; }
    }

    public class StatusActionRequestDto
    {
        public string? Remarks { get; set; }
        public string? ApprovedBy { get; set; }
    }

    public class DispatchPlanListItemDto
    {
        public int Id { get; set; }
        public string DispatchNumber { get; set; } = string.Empty;
        public string DispatchDate { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string SalesOrderNumber { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public DispatchPriority Priority { get; set; }
        public string PlannedDispatchDate { get; set; } = string.Empty;
        public string ExpectedDeliveryDate { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public bool VehicleRequired { get; set; }
        public DispatchPlanStatus Status { get; set; }
    }

    public class DispatchPlanItemDto
    {
        public string Id { get; set; } = string.Empty;
        public int FinishedGoodId { get; set; }
        public string FinishedGoodCode { get; set; } = string.Empty;
        public string FinishedGoodName { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Uom { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int? FinalInspectionId { get; set; }
        public string? FinalInspectionNumber { get; set; }
        public int? TestCertificateId { get; set; }
        public string? TestCertificateNumber { get; set; }
    }

    public class DispatchAttachmentDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int SizeKb { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public string UploadedAt { get; set; } = string.Empty;
        public string? Kind { get; set; }
    }

    public class DispatchTimelineEventDto
    {
        public string Id { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? FromStatus { get; set; }
        public string? ToStatus { get; set; }
        public string? Remarks { get; set; }
    }

    public class DispatchPlanDto
    {
        public int Id { get; set; }
        public string DispatchNumber { get; set; } = string.Empty;
        public string DispatchDate { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int SalesOrderId { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public DispatchPriority Priority { get; set; }
        public string PlannedDispatchDate { get; set; } = string.Empty;
        public string ExpectedDeliveryDate { get; set; } = string.Empty;
        public bool VehicleRequired { get; set; }
        public List<DispatchPlanItemDto> Items { get; set; } = new();
        public string Remarks { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<DispatchAttachmentDto> Attachments { get; set; } = new();
        public List<DispatchTimelineEventDto> Timeline { get; set; } = new();
        public DispatchPlanStatus Status { get; set; }
        public int? VehicleAssignmentId { get; set; }
        public int? TransportId { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }

    public class DispatchPlanItemCreateDto
    {
        public int FinishedGoodId { get; set; }
        public string FinishedGoodCode { get; set; } = string.Empty;
        public string FinishedGoodName { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Uom { get; set; } = "NOS";
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int? FinalInspectionId { get; set; }
        public string? FinalInspectionNumber { get; set; }
        public int? TestCertificateId { get; set; }
        public string? TestCertificateNumber { get; set; }
    }

    public class DispatchPlanCreateRequestDto
    {
        public string DispatchDate { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int SalesOrderId { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public DispatchPriority Priority { get; set; } = DispatchPriority.Normal;
        public string PlannedDispatchDate { get; set; } = string.Empty;
        public string ExpectedDeliveryDate { get; set; } = string.Empty;
        public bool VehicleRequired { get; set; } = true;
        public List<DispatchPlanItemCreateDto> Items { get; set; } = new();
        public string? Remarks { get; set; }
        public string? Notes { get; set; }
    }

    public class DispatchPlanUpdateRequestDto : DispatchPlanCreateRequestDto
    {
    }

    public class DispatchPlanDashboardDto
    {
        public int TodaysDispatches { get; set; }
        public int Planned { get; set; }
        public int Ready { get; set; }
        public int Completed { get; set; }
    }
}
