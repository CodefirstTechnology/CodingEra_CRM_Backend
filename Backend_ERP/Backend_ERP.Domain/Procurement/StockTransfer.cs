using System;
using System.Collections.Generic;

namespace ERP.Domain.Procurement
{
    public class StockTransfer
    {
        public int Id { get; set; }

        public string TransferNumber { get; set; } = string.Empty;

        public int FromWarehouseId { get; set; }

        public string FromWarehouseName { get; set; } = string.Empty;

        public int ToWarehouseId { get; set; }

        public string ToWarehouseName { get; set; } = string.Empty;

        public decimal TotalQuantity { get; set; }

        public TransferStatus Status { get; set; } = TransferStatus.Draft;

        public DateTime TransferDate { get; set; } = DateTime.UtcNow;

        public string RequestedBy { get; set; } = string.Empty;

        public string? ApprovedBy { get; set; }

        public string Remarks { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }

        public List<StockTransferItem> Items { get; set; } = new();
    }
}
