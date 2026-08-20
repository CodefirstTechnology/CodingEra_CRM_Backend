using System;
using System.Collections.Generic;

namespace ERP.Domain.Sales
{
    public enum LrStatus
    {
        Draft,
        Generated,
        Issued,
        Closed
    }

    public enum FreightPaymentType
    {
        Paid,
        ToPay,
        ToBeBilled
    }

    public enum EwayStatus
    {
        Draft,
        Generated,
        Active,
        Expired,
        Cancelled,
        Closed
    }

    public class LrDocumentSequence
    {
        public int Id { get; set; }

        public string Prefix { get; set; } = "LR";

        public int LastSequence { get; set; }
    }

    public class EwayDocumentSequence
    {
        public int Id { get; set; }

        public string Prefix { get; set; } = "EWB";

        public int LastSequence { get; set; }
    }

    public class LorryReceipt
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

        public DateTime LrDate { get; set; }

        public string Consignor { get; set; } = string.Empty;

        public string Consignee { get; set; } = string.Empty;

        public int Packages { get; set; }

        public decimal WeightKg { get; set; }

        public decimal FreightCharges { get; set; }

        public FreightPaymentType PaymentType { get; set; } = FreightPaymentType.ToBeBilled;

        public string Remarks { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public LrStatus Status { get; set; } = LrStatus.Draft;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<LrAttachment> Attachments { get; set; } = new List<LrAttachment>();

        public ICollection<LrTimelineEvent> Timeline { get; set; } = new List<LrTimelineEvent>();
    }

    public class LrAttachment
    {
        public int Id { get; set; }

        public int LorryReceiptId { get; set; }

        public LorryReceipt LorryReceipt { get; set; } = null!;

        public string AttachmentId { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; } = string.Empty;

        public int SizeKb { get; set; }

        public string UploadedBy { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public string? Kind { get; set; } = "document";
    }

    public class LrTimelineEvent
    {
        public int Id { get; set; }

        public int LorryReceiptId { get; set; }

        public LorryReceipt LorryReceipt { get; set; } = null!;

        public string EventId { get; set; } = Guid.NewGuid().ToString();

        public DateTime Date { get; set; } = DateTime.UtcNow;

        public string User { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public string? FromStatus { get; set; }

        public string? ToStatus { get; set; }

        public string? Remarks { get; set; }
    }

    public class EwayBill
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

        public DateTime ValidityFrom { get; set; }

        public DateTime ValidityTo { get; set; }

        public decimal DistanceKm { get; set; }

        public decimal TotalValue { get; set; }

        public string Remarks { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public EwayStatus Status { get; set; } = EwayStatus.Draft;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
