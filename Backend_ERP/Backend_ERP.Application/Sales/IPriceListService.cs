using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface IPriceListService
    {
        Task<IReadOnlyList<PriceListListItemDto>> GetAllAsync(
            PriceListListQueryDto? query,
            CancellationToken cancellationToken = default);

        Task<PriceListDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<PriceListDto> CreateAsync(
            PriceListCreateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<PriceListDto?> UpdateAsync(
            int id,
            PriceListUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, string actingUser, CancellationToken cancellationToken = default);

        Task<PriceListDto?> ActivateAsync(
            int id,
            PriceListRemarksRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<PriceListDto?> CloneAsync(
            int id,
            PriceListRemarksRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PriceListListItemDto>> GetActiveAsync(
            string? asOfDate,
            CancellationToken cancellationToken = default);

        Task<PriceListResolveDto?> ResolveAsync(
            PriceListResolveRequestDto request,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PriceListCompareDto>> CompareAsync(
            string? itemCode,
            string? customerCategory,
            CancellationToken cancellationToken = default);

        Task<PriceListDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

        Task<PriceListReportDto> GetReportsAsync(
            PriceListListQueryDto? query,
            CancellationToken cancellationToken = default);

        Task<PriceListExportMetadataDto> ExportReportsAsync(
            PriceListExportRequestDto request,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PriceListHistoryDto>?> GetHistoryAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> GetPermissionsAsync();

        Task<IReadOnlyList<PriceListLookupDto>> LookupItemsAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PriceListLookupDto>> LookupCustomerCategoriesAsync();

        Task<IReadOnlyList<PriceListLookupDto>> LookupCurrenciesAsync(CancellationToken cancellationToken = default);
    }
}
