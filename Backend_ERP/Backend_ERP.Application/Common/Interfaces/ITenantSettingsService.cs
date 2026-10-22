using System.Threading;
using System.Threading.Tasks;

namespace ERP.Application.Common.Interfaces
{
    public interface ITenantSettingsService
    {
        Task<string> GetTenantTimeZoneIdAsync(string tenantId, CancellationToken cancellationToken = default);
    }
}
