using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;

namespace ERP.Application.Sales.Quotations
{
    public interface IInventoryAllocationService
    {
        /// <summary>
        /// Non-blocking real-time Available-to-Promise (ATP) check for an item in a specific warehouse.
        /// </summary>
        Task<QuotationAtpCheckResponseDto> CheckAtpAsync(
            int warehouseId,
            int? itemId,
            string? itemCode,
            decimal requestedQuantity,
            string tenantId = "default",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks ATP across all line items of an active quotation.
        /// </summary>
        Task<IReadOnlyList<QuotationAtpCheckResponseDto>> CheckQuotationAtpAsync(
            int quotationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Row-locked soft reservations of inventory when a quotation is converted into a Sales Order.
        /// </summary>
        Task SoftReserveQuotationStockAsync(
            Quotation quotation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Releases soft reservations if a converted order is cancelled or rolled back.
        /// </summary>
        Task ReleaseQuotationStockReservationAsync(
            int quotationId,
            CancellationToken cancellationToken = default);
    }
}
