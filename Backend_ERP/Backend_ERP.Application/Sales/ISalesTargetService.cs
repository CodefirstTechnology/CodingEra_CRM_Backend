using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface ISalesTargetService
    {
        Task<IReadOnlyList<SalesTargetListItemDto>> GetAllAsync(
            SalesTargetListQueryDto? query,
            CancellationToken cancellationToken = default);

        Task<SalesTargetDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<SalesTargetDto> CreateAsync(
            SalesTargetCreateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<SalesTargetDto?> UpdateAsync(
            int id,
            SalesTargetUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, string actingUser, CancellationToken cancellationToken = default);

        Task<SalesTargetDto?> DuplicateAsync(
            int id,
            SalesTargetDuplicateRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<SalesTargetDto> CopyPreviousAsync(
            SalesTargetCopyPreviousRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<SalesTargetDto?> ActivateAsync(
            int id,
            SalesTargetRemarksRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<SalesTargetDto?> DeactivateAsync(
            int id,
            SalesTargetRemarksRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<SalesTargetDto?> UpdateStatusAsync(
            int id,
            SalesTargetStatusUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<SalesTargetDto?> UpdateProgressAsync(
            int id,
            SalesTargetProgressUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SalesTargetProgressDto>?> GetProgressAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SalesTargetHistoryDto>?> GetHistoryAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<SalesTargetDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

        Task<SalesTargetReportDto> GetReportsAsync(
            SalesTargetListQueryDto? query,
            CancellationToken cancellationToken = default);

        Task<SalesTargetExportMetadataDto> ExportReportsAsync(
            SalesTargetExportRequestDto request,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> GetPermissionsAsync();

        Task<IReadOnlyList<SalesTargetLookupDto>> LookupSalespersonsAsync(
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SalesTargetLookupDto>> LookupTeamsAsync(
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SalesTargetLookupDto>> LookupBranchesAsync(
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SalesTargetLookupDto>> LookupRegionalManagersAsync(
            CancellationToken cancellationToken = default);
    }
}
