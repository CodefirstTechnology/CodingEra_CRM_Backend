using System.Collections.Generic;
using ERP.Domain.Sales;

namespace ERP.Application.Sales.Dtos
{
    public class TransportListItemDto
    {
        public int Id { get; set; }
        public string TransportNumber { get; set; } = string.Empty;
        public string DispatchNumber { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string TransportCompanyName { get; set; } = string.Empty;
        public TransportMode Mode { get; set; } = TransportMode.Road;
        public string Route { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public decimal EstimatedTimeHours { get; set; }
        public TransportStatus Status { get; set; } = TransportStatus.Draft;
    }

    public class TransportCreateRequestDto
    {
        public int DispatchId { get; set; }
        public string DispatchNumber { get; set; } = string.Empty;
        public int VehicleAssignmentId { get; set; }
        public string AssignmentNumber { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string TransportCompanyName { get; set; } = string.Empty;
        public TransportMode Mode { get; set; } = TransportMode.Road;
        public string Route { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public decimal EstimatedTimeHours { get; set; }
        public string? FuelNotes { get; set; }
        public string? Remarks { get; set; }
        public string? Notes { get; set; }
    }

    public class TransportUpdateRequestDto : TransportCreateRequestDto
    {
    }

    public class TransportDashboardDto
    {
        public int VehiclesInTransit { get; set; }
        public int DeliveredToday { get; set; }
        public int DelayedShipments { get; set; }
    }

    public class LrListItemDto
    {
        public int Id { get; set; }
        public string LrNumber { get; set; } = string.Empty;
        public string DispatchNumber { get; set; } = string.Empty;
        public string TransportNumber { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string LrDate { get; set; } = string.Empty;
        public int Packages { get; set; }
        public decimal WeightKg { get; set; }
        public decimal FreightCharges { get; set; }
        public LrStatus Status { get; set; }
    }

    public class LrDto
    {
        public int Id { get; set; }
        public string LrNumber { get; set; } = string.Empty;
        public int DispatchId { get; set; }
        public string DispatchNumber { get; set; } = string.Empty;
        public int TransportId { get; set; }
        public string TransportNumber { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string LrDate { get; set; } = string.Empty;
        public string Consignor { get; set; } = string.Empty;
        public string Consignee { get; set; } = string.Empty;
        public int Packages { get; set; }
        public decimal WeightKg { get; set; }
        public decimal FreightCharges { get; set; }
        public FreightPaymentType PaymentType { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<DispatchAttachmentDto> Attachments { get; set; } = new();
        public List<DispatchTimelineEventDto> Timeline { get; set; } = new();
        public LrStatus Status { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }

    public class LrCreateRequestDto
    {
        public int DispatchId { get; set; }
        public string DispatchNumber { get; set; } = string.Empty;
        public int TransportId { get; set; }
        public string TransportNumber { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string LrDate { get; set; } = string.Empty;
        public string Consignor { get; set; } = string.Empty;
        public string Consignee { get; set; } = string.Empty;
        public int Packages { get; set; }
        public decimal WeightKg { get; set; }
        public decimal FreightCharges { get; set; }
        public FreightPaymentType PaymentType { get; set; } = FreightPaymentType.ToBeBilled;
        public string? Remarks { get; set; }
        public string? Notes { get; set; }
    }

    public class LrUpdateRequestDto : LrCreateRequestDto
    {
    }

    public class LrDashboardDto
    {
        public int Generated { get; set; }
        public int Issued { get; set; }
        public int Pending { get; set; }
    }

    public class EwayDto
    {
        public int Id { get; set; }
        public string EwayBillNumber { get; set; } = string.Empty;
        public int DispatchId { get; set; }
        public string DispatchNumber { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string GstNumber { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public int TransportId { get; set; }
        public string TransportNumber { get; set; } = string.Empty;
        public string ValidityFrom { get; set; } = string.Empty;
        public string ValidityTo { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public decimal TotalValue { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<DispatchAttachmentDto> Attachments { get; set; } = new();
        public List<DispatchTimelineEventDto> Timeline { get; set; } = new();
        public EwayStatus Status { get; set; } = EwayStatus.Generated;
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }

    public class EwayListItemDto
    {
        public int Id { get; set; }
        public string EwayBillNumber { get; set; } = string.Empty;
        public string DispatchNumber { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string GstNumber { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public string ValidityTo { get; set; } = string.Empty;
        public EwayStatus Status { get; set; } = EwayStatus.Draft;
    }

    public class EwayCreateRequestDto
    {
        public int DispatchId { get; set; }
        public string DispatchNumber { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string GstNumber { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public int TransportId { get; set; }
        public string TransportNumber { get; set; } = string.Empty;
        public string ValidityFrom { get; set; } = string.Empty;
        public string ValidityTo { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public string? Remarks { get; set; }
        public string? Notes { get; set; }
    }

    public class EwayUpdateRequestDto : EwayCreateRequestDto
    {
    }

    public class EwayExtendRequestDto
    {
        public string? ValidityTo { get; set; }
        public string? Remarks { get; set; }
    }

    public class EwayDashboardDto
    {
        public int ActiveBills { get; set; }
        public int ExpiringSoon { get; set; }
        public int Expired { get; set; }
    }
}
