using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;
using ERP.Shared.Models;

namespace ERP.Application.Procurement
{
    public interface IVendorQuotationService
    {
        Task<PagedResult<VendorQuotationDto>> GetAllAsync(int? rfqId, int? vendorId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

        Task<VendorQuotationDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<VendorQuotationDto> CreateAsync(VendorQuotationCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, string actingUser, CancellationToken cancellationToken = default);

        Task<string> GetNextQuotationNumberAsync(CancellationToken cancellationToken = default);
    }
}
