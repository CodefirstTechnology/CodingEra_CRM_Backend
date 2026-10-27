using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Dtos;

namespace ERP.Application.Sales
{
    public interface IProformaReportService
    {
        Task<IReadOnlyList<ProformaTaxVarianceReportDto>> GetTaxVarianceReportAsync(
            string? dateFrom = null,
            string? dateTo = null,
            CancellationToken ct = default);

        Task<IReadOnlyList<ProformaStateTaxProjectionReportDto>> GetStateTaxProjectionReportAsync(
            string? dateFrom = null,
            string? dateTo = null,
            CancellationToken ct = default);
    }
}
