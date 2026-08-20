using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Accounting.Dtos;

namespace ERP.Application.Accounting
{
    public interface IOutstandingService
    {
        Task<IReadOnlyList<OutstandingRowDto>> GetOutstandingAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<OutstandingRowDto>> GetCustomerOutstandingAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<OutstandingRowDto>> GetVendorOutstandingAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default);
        Task<AgeingReportDto> GetAgeingReportAsync(ListQueryDto? query = null, CancellationToken cancellationToken = default);
        Task<OutstandingDashboardDto> GetOutstandingDashboardAsync(CancellationToken cancellationToken = default);
        Task<OutstandingRowDto> RecordOrUpdateOutstandingAsync(OutstandingRowDto dto, string user, CancellationToken cancellationToken = default);
        Task<bool> ApplySettlementAsync(string partyType, string documentNumber, decimal amount, CancellationToken cancellationToken = default);
    }
}
