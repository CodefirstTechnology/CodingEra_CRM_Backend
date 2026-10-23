using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Domain.Sales;

namespace ERP.Application.Sales.Quotations
{
    public interface IQuotationRevisionService
    {
        /// <summary>
        /// Deep-clones a quotation to R+1 while locking the predecessor revision as Revised (IsCurrentRevision = false).
        /// </summary>
        Task<Quotation> CreateRevisionAsync(
            int quotationId,
            string? reason,
            string actingUser,
            CancellationToken cancellationToken = default);
    }
}
