namespace ERP.Application.Sales
{
    public interface IPriceListNumberingService
    {
        Task<string> GenerateNextPriceListNumberAsync(CancellationToken cancellationToken = default);
    }
}
