using System;
using ERP.Domain.Procurement;

namespace ERP.Domain.Sales
{
    public enum AllocationStatus
    {
        Reserved = 1,
        Staged = 2,
        Dispatched = 3,
        Released = 4
    }

    public class SalesOrderAllocation
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = "default";

        public int SalesOrderId { get; set; }

        public SalesOrder SalesOrder { get; set; } = null!;

        public int SalesOrderItemId { get; set; }

        public SalesOrderItem SalesOrderItem { get; set; } = null!;

        public int FinishedGoodId { get; set; }

        public FinishedGood FinishedGood { get; set; } = null!;

        public int WarehouseId { get; set; }

        public Warehouse Warehouse { get; set; } = null!;

        public string BatchNumber { get; set; } = string.Empty;

        public decimal AllocatedQuantity { get; set; }

        public AllocationStatus AllocationStatus { get; set; } = AllocationStatus.Reserved;

        public DateTimeOffset AllocatedAt { get; set; } = DateTimeOffset.UtcNow;

        public string AllocatedBy { get; set; } = string.Empty;

        public DateTimeOffset? ReleasedDate { get; set; }

        public string? ReleaseRemarks { get; set; }

        /// <summary>
        /// PostgreSQL xmin concurrency token.
        /// </summary>
        public uint Version { get; set; }
    }
}
