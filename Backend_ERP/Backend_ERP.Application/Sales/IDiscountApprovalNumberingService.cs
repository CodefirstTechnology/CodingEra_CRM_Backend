namespace ERP.Application.Sales
{
    public interface IDiscountApprovalNumberingService
    {
        Task<string> GenerateNextApprovalNumberAsync(CancellationToken cancellationToken = default);
    }
}
