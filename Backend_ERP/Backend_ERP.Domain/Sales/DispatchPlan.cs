using System;
using System.Collections.Generic;

namespace ERP.Domain.Sales
{
    public class DispatchPlan
    {
        public int Id { get; set; }

        public string DispatchNumber { get; set; } = string.Empty;

        public DateTime DispatchDate { get; set; }

        public int CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public int SalesOrderId { get; set; }

        public string SalesOrderNumber { get; set; } = string.Empty;

        public string DeliveryAddress { get; set; } = string.Empty;

        public int WarehouseId { get; set; }

        public string WarehouseName { get; set; } = string.Empty;

        public DispatchPriority Priority { get; set; } = DispatchPriority.Normal;

        public DateTime PlannedDispatchDate { get; set; }

        public DateTime ExpectedDeliveryDate { get; set; }

        public bool VehicleRequired { get; set; } = true;

        public string Remarks { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public DispatchPlanStatus Status { get; set; } = DispatchPlanStatus.Draft;

        public int? VehicleAssignmentId { get; set; }

        public int? TransportId { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<DispatchPlanItem> Items { get; set; } = new List<DispatchPlanItem>();

        public ICollection<DispatchPlanAttachment> Attachments { get; set; } = new List<DispatchPlanAttachment>();

        public ICollection<DispatchPlanTimelineEvent> Timeline { get; set; } = new List<DispatchPlanTimelineEvent>();
    }
}
