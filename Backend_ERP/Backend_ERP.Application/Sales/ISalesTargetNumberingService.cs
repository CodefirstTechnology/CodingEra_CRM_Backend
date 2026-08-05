namespace ERP.Application.Sales
{
    public interface ISalesTargetNumberingService
    {
        Task<string> GenerateNextTargetNumberAsync(CancellationToken cancellationToken = default);
    }
}
