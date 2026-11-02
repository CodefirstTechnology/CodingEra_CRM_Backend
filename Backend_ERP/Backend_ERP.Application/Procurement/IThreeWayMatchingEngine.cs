using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement
{
    public interface IThreeWayMatchingEngine
    {
        Task<PurchaseBill3WayMatchResultDto> EvaluateMatchAsync(
            PurchaseBillCreateRequestDto request,
            CancellationToken cancellationToken = default);

        Task<PurchaseBill3WayMatchResultDto> ProcessBillPostingValidationAsync(
            int purchaseBillId,
            CancellationToken cancellationToken = default);

        Task<bool> AuthorizeVarianceOverrideAsync(
            int purchaseBillId,
            string overrideReason,
            string managerUserId,
            CancellationToken cancellationToken = default);
    }
}

