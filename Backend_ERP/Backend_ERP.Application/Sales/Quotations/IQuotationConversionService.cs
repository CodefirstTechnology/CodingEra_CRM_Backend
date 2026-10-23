using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales.Quotations
{
    public interface IQuotationConversionService
    {
        /// <summary>
        /// Promotes an approved quotation to a Sales Order under pessimistic row lock ('FOR UPDATE'),
        /// transferring frozen snapshots with zero recalculation and triggering soft inventory reservations.
        /// </summary>
        Task<SalesOrderDto> ConvertQuotationToSalesOrderAsync(
            int quotationId,
            string actingUser,
            CancellationToken cancellationToken = default);
    }
}
