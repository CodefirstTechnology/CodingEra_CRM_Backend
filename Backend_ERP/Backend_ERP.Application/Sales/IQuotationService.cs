using ERP.Shared.Models;
using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface IQuotationService
    {
        Task<PagedResult<QuotationDto>> GetPagedAsync(
            QuotationListQueryDto query,
            CancellationToken cancellationToken = default);

        Task<QuotationDto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<QuotationDto> CreateAsync(
            QuotationCreateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<QuotationDto?> UpdateAsync(
            int id,
            QuotationUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(
            int id,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<QuotationDto> MarkClientAcceptedAsync(
            int id,
            QuotationClientAcceptRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<QuotationDto> ReviseAsync(
            int id,
            QuotationReviseRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<QuotationDto> DuplicateAsync(
            int id,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<string> GetNextNumberAsync(CancellationToken cancellationToken = default);
    }
}
