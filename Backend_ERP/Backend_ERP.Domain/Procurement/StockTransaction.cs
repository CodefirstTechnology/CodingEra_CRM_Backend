using System;

namespace ERP.Domain.Procurement
{
    public class StockTransaction
    {
        public int Id { get; set; }

        public string TransactionNumber { get; set; } = string.Empty;

        public StockTxnType TransactionType { get; set; } = StockTxnType.StockIn;

        public int? MaterialId { get; set; }

        public string MaterialCode { get; set; } = string.Empty;

        public string MaterialName { get; set; } = string.Empty;

        public int WarehouseId { get; set; }

        public string WarehouseName { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public string Unit { get; set; } = "Nos";

        public string Reason { get; set; } = string.Empty;

        public StockReferenceType ReferenceType { get; set; } = StockReferenceType.None;

        public string ReferenceNumber { get; set; } = string.Empty;

        public int? ReferenceId { get; set; }

        public string User { get; set; } = string.Empty;

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        public string Remarks { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }
    }
}
