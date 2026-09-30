namespace ERP.Domain.Procurement
{
    public class StockTransferItem
    {
        public int Id { get; set; }

        public int StockTransferId { get; set; }

        public StockTransfer? StockTransfer { get; set; }

        public int MaterialId { get; set; }

        public string MaterialCode { get; set; } = string.Empty;

        public string MaterialName { get; set; } = string.Empty;

        public string Unit { get; set; } = "Nos";

        public decimal Quantity { get; set; }

        public string? BatchNumber { get; set; }

        public Guid BatchId { get; set; }

        public Guid OriginBinId { get; set; }

        public Guid? DestinationBinId { get; set; }

        public decimal RequestedQty { get; set; }

        public decimal DispatchedQty { get; set; }

        public decimal ReceivedQty { get; set; }

        public decimal DamagedQty { get; set; }

        public string? VarianceReason { get; set; }
    }
}
