using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales.Pricing
{
    public interface IBomMarginAuditService
    {
        Task<BomMarginAuditDto> AuditItemMarginAsync(
            string itemCode,
            decimal proposedSellingRate,
            CancellationToken cancellationToken = default);
    }
}
