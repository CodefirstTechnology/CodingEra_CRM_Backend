using System;
using System.Collections.Generic;

namespace ERP.Domain.Sales
{
    public enum LrStatus
    {
        Draft,
        Issued,
        Acknowledged,
        Closed
    }

    public enum FreightPaymentType
    {
        Paid,
        ToPay,
        ToBeBilled
    }

    public class LrDocumentSequence
    {
        public int Id { get; set; }

        public string Prefix { get; set; } = "LR";

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
    }
}
