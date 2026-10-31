using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;
using ERP.Shared.Models;

namespace ERP.Application.Procurement
{
    public interface IVendorService
    {
        Task<PagedResult<VendorListItemDto>> GetAllAsync(VendorListQueryDto query, CancellationToken cancellationToken = default);

        Task<VendorDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<VendorDto> CreateAsync(VendorCreateRequestDto request, string actingUser, CancellationToken cancellationToken = default);

        Task<VendorDto?> UpdateAsync(int id, VendorUpdateRequestDto request, string actingUser, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, string actingUser, CancellationToken cancellationToken = default);

        Task<VendorDto?> ActivateAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default);

        Task<VendorDto?> DeactivateAsync(int id, string remarks, string actingUser, CancellationToken cancellationToken = default);

        Task<VendorDto?> UpdateStatusAsync(int id, VendorStatusUpdateRequestDto request, string actingUser, CancellationToken cancellationToken = default);

        Task<string> GetNextVendorCodeAsync(CancellationToken cancellationToken = default);

        Task<VendorPerformanceSummaryDto?> GetPerformanceSummaryAsync(int id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> GetPermissionsAsync();

        // KYC Verification Workflow
        Task<VendorDto?> SubmitForKycApprovalAsync(int id, string actingUser, CancellationToken cancellationToken = default);

        Task<VendorDto?> ApproveVendorKycAsync(int id, VendorKycApprovalRequestDto request, string actingUser, CancellationToken cancellationToken = default);

        Task<VendorDto?> RejectVendorKycAsync(int id, string reason, string actingUser, CancellationToken cancellationToken = default);

        // Bank Account Management & Penny Drop
        Task<VendorBankAccountDto> AddBankAccountAsync(int vendorId, VendorBankAccountDto dto, string actingUser, CancellationToken cancellationToken = default);

        Task<VendorBankAccountDto?> UpdateBankAccountAsync(int vendorId, int bankAccountId, VendorBankAccountDto dto, string actingUser, CancellationToken cancellationToken = default);

        Task<bool> DeleteBankAccountAsync(int vendorId, int bankAccountId, string actingUser, CancellationToken cancellationToken = default);

        Task<VendorBankAccountDto?> VerifyBankAccountPennyDropAsync(int vendorId, int bankAccountId, string actingUser, CancellationToken cancellationToken = default);
    }
}
