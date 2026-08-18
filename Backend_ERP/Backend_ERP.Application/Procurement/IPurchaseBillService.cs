using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;
using ERP.Shared.Models;

namespace ERP.Application.Procurement
{
    public interface IPurchaseBillService
    {
        Task<PagedResult<PurchaseBillDto>> GetPurchaseBillsAsync(PurchaseBillFilterQueryDto query, CancellationToken cancellationToken = default);
        Task<PurchaseBillDto?> GetPurchaseBillByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<PurchaseBillDto> CreatePurchaseBillAsync(PurchaseBillCreateRequestDto request, string currentUser, CancellationToken cancellationToken = default);
        Task<PurchaseBillDto?> UpdatePurchaseBillAsync(int id, PurchaseBillCreateRequestDto request, string currentUser, CancellationToken cancellationToken = default);
        Task<bool> DeletePurchaseBillAsync(int id, string currentUser, CancellationToken cancellationToken = default);
        Task<PurchaseBillDto?> ApprovePurchaseBillAsync(int id, string remarks, string currentUser, CancellationToken cancellationToken = default);
        Task<PurchaseBillDto?> PostPurchaseBillAsync(int id, string remarks, string currentUser, CancellationToken cancellationToken = default);
        Task<PurchaseBillDto?> PayPurchaseBillAsync(int id, string remarks, string currentUser, CancellationToken cancellationToken = default);
        Task<PurchaseBillDto?> VoidPurchaseBillAsync(int id, string remarks, string currentUser, CancellationToken cancellationToken = default);
        Task<PurchaseBillDto?> DuplicatePurchaseBillAsync(int id, string currentUser, CancellationToken cancellationToken = default);
        Task<PurchaseBillDashboardDto> GetPurchaseBillDashboardAsync(CancellationToken cancellationToken = default);
    }
}
