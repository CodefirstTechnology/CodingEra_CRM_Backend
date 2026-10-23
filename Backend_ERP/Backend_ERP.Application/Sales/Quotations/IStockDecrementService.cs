using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ERP.Application.Sales.Quotations
{
    public class StockDeductionItemDto
    {
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal QuantityDeducted { get; set; }
        public decimal RemainingPhysicalStock { get; set; }
        public string TransactionNumber { get; set; } = string.Empty;
    }

    public class StockDeductionResultDto
    {
        public int SalesOrderId { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;
        public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;
        public List<StockDeductionItemDto> DeductedItems { get; set; } = new();
    }

    public interface IStockDecrementService
    {
        /// <summary>
        /// Row-locked serializable physical stock reduction upon sales order dispatch completion.
        /// Decrements FinishedQuantity, releases soft-reservation, prevents negative stock,
        /// and records double-entry immutable stock transactions.
        /// </summary>
        Task<StockDeductionResultDto> DeductPhysicalStockForSalesOrderAsync(
            int salesOrderId,
            string actingUser,
            CancellationToken cancellationToken = default);
    }
}
