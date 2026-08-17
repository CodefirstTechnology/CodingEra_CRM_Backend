using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;
using ERP.Shared.Models;

namespace ERP.Application.Procurement
{
    public interface IVendorComparisonService
    {
        Task<PagedResult<VendorComparisonListItemDto>> GetAllAsync(VendorComparisonListQueryDto query, CancellationToken cancellationToken = default);

        Task<VendorComparisonDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<VendorComparisonDto> CreateAsync(VendorComparisonCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default);

        Task<VendorComparisonDto?> UpdateAsync(int id, VendorComparisonUpdateRequestDto request, string actingUser, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, string actingUser, CancellationToken cancellationToken = default);

        Task<VendorComparisonDto?> MarkComparedAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default);

        Task<VendorComparisonDto?> ApproveAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default);

        Task<VendorComparisonDto?> AwardVendorAsync(int id, VendorAwardRequestDto request, string actingUser, CancellationToken cancellationToken = default);

        Task<string> GetNextComparisonNumberAsync(CancellationToken cancellationToken = default);
    }
}
