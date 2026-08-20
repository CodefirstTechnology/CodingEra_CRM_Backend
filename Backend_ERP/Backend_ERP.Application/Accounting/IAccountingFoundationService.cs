using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ERP.Application.Accounting
{
    public interface IAccountingFoundationService
    {
        Task<IReadOnlyList<string>> GetPermissionsAsync(CancellationToken cancellationToken = default);
        Task<string> GenerateCustomerLedgerNumberAsync(CancellationToken cancellationToken = default);
        Task<string> GenerateGstTransactionNumberAsync(CancellationToken cancellationToken = default);
        Task<string> GeneratePaymentNumberAsync(CancellationToken cancellationToken = default);
        Task<string> GenerateReceiptNumberAsync(CancellationToken cancellationToken = default);
        Task<string> GenerateBankReconNumberAsync(CancellationToken cancellationToken = default);
    }
}
