using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface IAdvancePaymentSettlementService
    {
        Task<AdvancePaymentDto> ClearPaymentAsync(int paymentId, AdvancePaymentClearRequestDto request, string actingUser, CancellationToken cancellationToken = default);
        Task<AdvancePaymentDto> DishonorPaymentAsync(int paymentId, AdvancePaymentDishonorRequestDto request, string actingUser, CancellationToken cancellationToken = default);
        Task<AdvancePaymentAdjustmentDto> AdjustAgainstTaxInvoiceAsync(AdvancePaymentAdjustmentRequestDto request, string actingUser, CancellationToken cancellationToken = default);
        Task SynchronizeOperationalGatesAsync(int salesOrderId, CancellationToken cancellationToken = default);
    }
}
