using System;

namespace ERP.Domain.Procurement
{
    public class Warehouse
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string Manager { get; set; } = string.Empty;

        public decimal Capacity { get; set; }

        public decimal UsedCapacity { get; set; }

        public decimal AvailableCapacity { get; set; }

        public WarehouseStatus Status { get; set; } = WarehouseStatus.Active;

        public int AssignedInventoryCount { get; set; }

        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }
    }
}
