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
    }
}
