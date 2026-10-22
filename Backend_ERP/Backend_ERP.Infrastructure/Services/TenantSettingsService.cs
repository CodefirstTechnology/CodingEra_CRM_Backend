using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Common.Interfaces;

namespace ERP.Infrastructure.Services
{
    public class TenantSettingsService : ITenantSettingsService
    {
        public Task<string> GetTenantTimeZoneIdAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            // Default to India Standard Time (IST / Asia/Kolkata) as standard enterprise operational jurisdiction
            // or UTC based on tenant profile
            return Task.FromResult("India Standard Time");
        }
    }
}
