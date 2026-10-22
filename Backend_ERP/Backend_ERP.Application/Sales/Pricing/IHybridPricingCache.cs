using System;
using System.Threading.Tasks;

namespace ERP.Application.Sales.Pricing
{
    public interface IHybridPricingCache
    {
        Task<T> GetOrCreateAsync<T>(string key, string tenantId, Func<Task<T>> factory, TimeSpan ttl);
        Task InvalidateTenantItemAsync(string tenantId, string itemCode, string currency);
    }
}
