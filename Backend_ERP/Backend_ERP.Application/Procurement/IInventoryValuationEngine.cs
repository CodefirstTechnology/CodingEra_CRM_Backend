using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;

namespace ERP.Application.Procurement
{
    public interface IInventoryValuationEngine
    {
        Task<InventoryValuationResultDto> ProcessGrnInventoryValuationAsync(
            int goodsReceiptId,
            string actingUser,
            CancellationToken cancellationToken = default);

        decimal CalculateMovingWac(
            decimal currentStock,
            decimal currentWac,
            decimal acceptedQuantity,
            decimal landedUnitCost);
    }
}

