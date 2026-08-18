using System;

namespace ERP.Domain.Procurement
{
    public class StockAlert
    {
        public int Id { get; set; }

        public int MaterialId { get; set; }

        public string MaterialCode { get; set; } = string.Empty;

        public string MaterialName { get; set; } = string.Empty;

        public int WarehouseId { get; set; }

        public string WarehouseName { get; set; } = string.Empty;

        public decimal CurrentStock { get; set; }

        public decimal MinimumStock { get; set; }

        public decimal ReorderQuantity { get; set; }

        public string Unit { get; set; } = "Nos";

        public AlertPriority Priority { get; set; } = AlertPriority.Warning;

        public bool SuggestedPurchase { get; set; } = true;

        public int? LinkedRequisitionId { get; set; }

        public string? LinkedRequisitionNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
