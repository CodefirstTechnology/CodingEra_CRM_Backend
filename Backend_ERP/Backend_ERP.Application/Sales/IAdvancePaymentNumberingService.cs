namespace ERP.Application.Sales
{
    public interface IAdvancePaymentNumberingService
    {
        Task<string> GenerateNextPaymentNumberAsync(CancellationToken cancellationToken = default);
    }
}
