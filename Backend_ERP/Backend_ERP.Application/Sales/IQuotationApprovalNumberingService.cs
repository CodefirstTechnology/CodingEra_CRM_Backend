namespace ERP.Application.Sales
{
    public interface IQuotationApprovalNumberingService
    {
        Task<string> GenerateNextApprovalNumberAsync(CancellationToken cancellationToken = default);
    }
}
