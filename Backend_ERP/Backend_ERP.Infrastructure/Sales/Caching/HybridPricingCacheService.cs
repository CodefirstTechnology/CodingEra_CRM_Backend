using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales.Pricing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ERP.Infrastructure.Sales.Caching
{
    public class HybridPricingCacheService : IHybridPricingCache
    {
        private readonly IMemoryCache _l1;
        private readonly IConnectionMultiplexer? _redis;
        private readonly ILogger<HybridPricingCacheService> _logger;
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _keyedLocks = new();

        public HybridPricingCacheService(
            IMemoryCache l1,
            ILogger<HybridPricingCacheService> logger,
            IConnectionMultiplexer? redis = null)
        {
            _l1 = l1;
            _logger = logger;
            _redis = redis;
        }

        public async Task<T> GetOrCreateAsync<T>(string key, string tenantId, Func<Task<T>> factory, TimeSpan ttl)
        {
            // 1. Check L1 Memory Cache (Sub-microsecond)
            if (_l1.TryGetValue(key, out T? l1Val) && l1Val != null)
            {
                return l1Val;
            }

            // 2. Anti-Stampede Semaphore per Key
            var semaphore = _keyedLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();

            try
            {
                // Double check L1 inside critical section
                if (_l1.TryGetValue(key, out l1Val) && l1Val != null)
                {
                    return l1Val;
                }

                // 3. Check L2 Redis if available
                if (_redis != null && _redis.IsConnected)
                {
                    try
                    {
                        var db = _redis.GetDatabase();
                        var cached = await db.StringGetAsync(key);
                        if (cached.HasValue)
                        {
                            var deserialized = JsonSerializer.Deserialize<T>(cached.ToString()!);
                            if (deserialized != null)
                            {
                                _l1.Set(key, deserialized, TimeSpan.FromMinutes(2));
                                return deserialized;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Redis cache read failed for key {Key}. Proceeding to database.", key);
                    }
                }

                // 4. Cache Miss: Execute Factory (PostgreSQL)
                var result = await factory();

                // 5. Populate L1 and L2
                _l1.Set(key, result, TimeSpan.FromMinutes(2));

                if (_redis != null && _redis.IsConnected && result != null)
                {
                    try
                    {
                        var db = _redis.GetDatabase();
                        var json = JsonSerializer.Serialize(result);
                        await db.StringSetAsync(key, json, ttl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Redis cache write failed for key {Key}.", key);
                    }
                }

                return result;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task InvalidateTenantItemAsync(string tenantId, string itemCode, string currency)
        {
            if (_redis != null && _redis.IsConnected)
            {
                try
                {
                    var server = _redis.GetServer(_redis.GetEndPoints()[0]);
                    var searchPattern = $"erp:pricing:v1:{tenantId}:*:*:{itemCode}:*";
                    var keys = server.Keys(pattern: searchPattern);

                    var db = _redis.GetDatabase();
                    foreach (var key in keys)
                    {
                        await db.KeyDeleteAsync(key);
                    }

                    // Tenant-Scoped Redis Pub/Sub Channel
                    var sub = _redis.GetSubscriber();
                    var payload = JsonSerializer.Serialize(new
                    {
                        TenantId = tenantId,
                        ItemCode = itemCode,
                        Currency = currency,
                        InvalidatedAtUtc = DateTimeOffset.UtcNow
                    });

                    await sub.PublishAsync(
                        RedisChannel.Literal($"erp:pricing:{tenantId}:cache:invalidate"),
                        payload);

                    _logger.LogInformation("Evicted Redis cache for tenant {TenantId}, item {ItemCode}", tenantId, itemCode);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to invalidate Redis cache for tenant {TenantId}, item {ItemCode}", tenantId, itemCode);
                }
            }
        }
    }
}
