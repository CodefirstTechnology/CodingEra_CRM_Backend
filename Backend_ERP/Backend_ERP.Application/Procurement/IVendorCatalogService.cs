using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;

namespace ERP.Application.Procurement
{
    public interface IVendorCatalogService
    {
        Task<IReadOnlyList<VendorItemCatalogDto>> GetVendorCatalogsAsync(int vendorId, CancellationToken cancellationToken = default);

        Task<VendorItemCatalogDto> AddCatalogItemAsync(int vendorId, VendorItemCatalogCreateDto dto, string actingUser, CancellationToken cancellationToken = default);

        Task<VendorItemCatalogDto?> UpdateCatalogItemAsync(int vendorId, int catalogId, VendorItemCatalogUpdateDto dto, string actingUser, CancellationToken cancellationToken = default);

        Task<bool> DeleteCatalogItemAsync(int vendorId, int catalogId, string actingUser, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<VendorItemCatalogDto>> GetApprovedVendorsForItemAsync(string itemCode, CancellationToken cancellationToken = default);

        Task<SourcingGateValidationResultDto> ValidateItemSourcingGateAsync(string tenantId, int vendorId, string itemCode, decimal orderQuantity, CancellationToken cancellationToken = default);
    }
}

