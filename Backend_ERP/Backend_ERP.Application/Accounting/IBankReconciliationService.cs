using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Accounting.Dtos;

namespace ERP.Application.Accounting
{
    public interface IBankReconciliationService
    {
        Task<IReadOnlyList<BankReconListItemDto>> GetBankReconciliationsAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default);
        Task<BankReconciliationDto?> GetBankReconciliationByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<BankReconciliationDto> CreateBankReconciliationAsync(BankReconCreateRequestDto dto, string user, CancellationToken cancellationToken = default);
        Task<BankReconciliationDto> UpdateBankReconciliationAsync(int id, BankReconUpdateRequestDto dto, string user, CancellationToken cancellationToken = default);
        Task<BankReconciliationDto> VerifyBankReconciliationAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default);
        Task<BankReconciliationDto> ReconcileBankAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default);
        Task<BankReconciliationDto> CloseBankReconciliationAsync(int id, StatusActionRequestDto? payload, string user, CancellationToken cancellationToken = default);
        Task<BankReconDashboardDto> GetBankReconciliationDashboardAsync(CancellationToken cancellationToken = default);
    }
}
