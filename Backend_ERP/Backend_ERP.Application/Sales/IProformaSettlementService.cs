using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface IProformaSettlementService
    {
        Task<ProformaSettlementResultDto> SettleReceiptAsync(
            int proformaInvoiceId,
            ProformaSettlementRequestDto request,
            string actingUser,
            CancellationToken ct = default);

        Task<bool> ReverseSettlementAsync(
            int settlementAllocationId,
            string reason,
            string actingUser,
            CancellationToken ct = default);
    }
}
