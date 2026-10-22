using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Pricing;
using ERP.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ERP.Infrastructure.Data.Interceptors
{
    public class PricingTransactionCommitInterceptor : SaveChangesInterceptor
    {
        private readonly IHybridPricingCache _cache;
        private readonly List<(string TenantId, string ItemCode, string Currency)> _pendingInvalidations = new();

        public PricingTransactionCommitInterceptor(IHybridPricingCache cache)
        {
            _cache = cache;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context != null)
            {
                foreach (var entry in eventData.Context.ChangeTracker.Entries<PriceList>())
                {
                    if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
                    {
                        foreach (var item in entry.Entity.Items)
                        {
                            _pendingInvalidations.Add((entry.Entity.TenantId, item.ItemCode, entry.Entity.Currency));
                        }
                    }
                }
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            // Executed strictly after successful database commit
            foreach (var (tenantId, itemCode, currency) in _pendingInvalidations)
            {
                await _cache.InvalidateTenantItemAsync(tenantId, itemCode, currency);
            }

            _pendingInvalidations.Clear();
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }
    }
}
