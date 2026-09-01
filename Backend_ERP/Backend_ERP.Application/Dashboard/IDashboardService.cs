using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Dashboard.Dtos;

namespace ERP.Application.Dashboard
{
    public interface IDashboardService
    {
        Task<AdminDashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    }
}
