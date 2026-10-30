using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ERP.Infrastructure.Sales.Caching
{
    public interface ITargetRedisCacheManager
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan slidingExpiration);
        Task InvalidatePatternAsync(string pattern);
        Task InvalidateTenantAsync(string tenantId);
    }

    public class TargetRedisCacheManager : ITargetRedisCacheManager
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IConnectionMultiplexer? _redis;
        private readonly ILogger<TargetRedisCacheManager> _logger;

        public TargetRedisCacheManager(
            IMemoryCache? memoryCache = null,
            ILogger<TargetRedisCacheManager>? logger = null,
            IConnectionMultiplexer? redis = null)
        {
            _memoryCache = memoryCache ?? new MemoryCache(new MemoryCacheOptions());
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<TargetRedisCacheManager>.Instance;
            _redis = redis;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            // 1. Check L1 in-process memory cache
            if (_memoryCache.TryGetValue(key, out T? cachedValue) && cachedValue != null)
            {
                return cachedValue;
            }

            // 2. Check L2 Redis cache if connected
            if (_redis != null && _redis.IsConnected)
            {
                try
                {
                    var db = _redis.GetDatabase();
                    var redisVal = await db.StringGetAsync(key);
                    if (redisVal.HasValue)
                    {
                        var deserialized = JsonSerializer.Deserialize<T>(redisVal.ToString());
                        if (deserialized != null)
                        {
                            _memoryCache.Set(key, deserialized, TimeSpan.FromSeconds(30));
                            return deserialized;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read key {Key} from Redis cache. Falling back.", key);
                }
            }

            return default;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan slidingExpiration)
        {
            if (value == null) return;

            // 1. Set in L1 memory cache
            _memoryCache.Set(key, value, slidingExpiration);

            // 2. Set in L2 Redis if available
            if (_redis != null && _redis.IsConnected)
            {
                try
                {
                    var db = _redis.GetDatabase();
                    var json = JsonSerializer.Serialize(value);
                    await db.StringSetAsync(key, json, slidingExpiration);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to write key {Key} to Redis cache.", key);
                }
            }
        }

        public async Task InvalidatePatternAsync(string pattern)
        {
            if (_redis != null && _redis.IsConnected)
            {
                try
                {
                    var endpoints = _redis.GetEndPoints();
                    foreach (var endpoint in endpoints)
                    {
                        var server = _redis.GetServer(endpoint);
                        if (server.IsConnected)
                        {
                            var keys = server.Keys(pattern: pattern);
                            var db = _redis.GetDatabase();
                            foreach (var key in keys)
                            {
                                await db.KeyDeleteAsync(key);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error while purging pattern {Pattern} from Redis.", pattern);
                }
            }
        }

        public async Task InvalidateTenantAsync(string tenantId)
        {
            await InvalidatePatternAsync($"erp:{tenantId}:targets:*");
        }
    }
}

