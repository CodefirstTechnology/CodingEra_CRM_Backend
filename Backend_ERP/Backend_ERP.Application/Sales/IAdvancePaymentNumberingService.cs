namespace ERP.Application.Sales
{
    public interface IAdvancePaymentNumberingService
    {
        Task<string> GenerateNextPaymentNumberAsync(CancellationToken cancellationToken = default);
        Task<string> GenerateNextReceiptVoucherNumberAsync(CancellationToken cancellationToken = default);
        Task<string> GenerateNextRefundVoucherNumberAsync(CancellationToken cancellationToken = default);
    }
}
