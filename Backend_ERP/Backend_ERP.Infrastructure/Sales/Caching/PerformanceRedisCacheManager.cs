using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Sales;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ERP.Infrastructure.Sales.Caching
{
    public class PerformanceRedisCacheManager : IPerformanceRedisCacheManager
    {
        private readonly IConnectionMultiplexer? _redis;
        private readonly ILogger<PerformanceRedisCacheManager> _logger;
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<int, double>> _inMemoryZSets = new();

        public PerformanceRedisCacheManager(
            IConnectionMultiplexer? redis = null,
            ILogger<PerformanceRedisCacheManager>? logger = null)
        {
            _redis = redis;
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PerformanceRedisCacheManager>.Instance;
        }

        private static string BuildKey(string tenantId, int financialYear, string metric) =>
            $"erp:{tenantId}:perf:leaderboard:{financialYear}:{metric.ToLowerInvariant()}";

        public async Task UpdateLeaderboardScoreAsync(
            string tenantId,
            int financialYear,
            string metric,
            int salesPersonUserId,
            double score,
            CancellationToken ct = default)
        {
            var key = BuildKey(tenantId, financialYear, metric);

            // 1. In-Memory fallback store update
            var dict = _inMemoryZSets.GetOrAdd(key, _ => new ConcurrentDictionary<int, double>());
            dict[salesPersonUserId] = score;

            // 2. Redis ZSET update if connected
            if (_redis != null && _redis.IsConnected)
            {
                try
                {
                    var db = _redis.GetDatabase();
                    await db.SortedSetAddAsync(key, salesPersonUserId.ToString(), score);
                    await db.KeyExpireAsync(key, TimeSpan.FromHours(24));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update Redis sorted set for {Key}", key);
                }
            }
        }

        public async Task<List<(int SalesPersonUserId, double Score)>> GetTopRanksAsync(
            string tenantId,
            int financialYear,
            string metric,
            int count = 50,
            CancellationToken ct = default)
        {
            var key = BuildKey(tenantId, financialYear, metric);

            if (_redis != null && _redis.IsConnected)
            {
                try
                {
                    var db = _redis.GetDatabase();
                    var entries = await db.SortedSetRangeByRankWithScoresAsync(key, 0, count - 1, Order.Descending);
                    if (entries != null && entries.Length > 0)
                    {
                        var result = new List<(int, double)>();
                        foreach (var entry in entries)
                        {
                            if (int.TryParse(entry.Element, out int userId))
                            {
                                result.Add((userId, entry.Score));
                            }
                        }
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to query Redis sorted set for {Key}. Using memory fallback.", key);
                }
            }

            // Fallback to in-memory dictionary
            if (_inMemoryZSets.TryGetValue(key, out var inMemDict))
            {
                return inMemDict
                    .OrderByDescending(kv => kv.Value)
                    .Take(count)
                    .Select(kv => (kv.Key, kv.Value))
                    .ToList();
            }

            return new List<(int, double)>();
        }

        public async Task<long?> GetUserRankAsync(
            string tenantId,
            int financialYear,
            string metric,
            int salesPersonUserId,
            CancellationToken ct = default)
        {
            var key = BuildKey(tenantId, financialYear, metric);

            if (_redis != null && _redis.IsConnected)
            {
                try
                {
                    var db = _redis.GetDatabase();
                    var rank = await db.SortedSetRankAsync(key, salesPersonUserId.ToString(), Order.Descending);
                    if (rank.HasValue)
                    {
                        return rank.Value + 1; // 1-based index
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to query user rank from Redis for {Key}", key);
                }
            }

            // Fallback to memory
            if (_inMemoryZSets.TryGetValue(key, out var inMemDict))
            {
                var sorted = inMemDict.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToList();
                var index = sorted.IndexOf(salesPersonUserId);
                if (index >= 0) return index + 1;
            }

            return null;
        }

        public async Task InvalidateLeaderboardAsync(
            string tenantId,
            int financialYear,
            CancellationToken ct = default)
        {
            var prefix = $"erp:{tenantId}:perf:leaderboard:{financialYear}:";

            // Clear in-memory
            var keysToRemove = _inMemoryZSets.Keys.Where(k => k.StartsWith(prefix)).ToList();
            foreach (var k in keysToRemove)
            {
                _inMemoryZSets.TryRemove(k, out _);
            }

            // Clear Redis
            if (_redis != null && _redis.IsConnected)
            {
                try
                {
                    var endpoints = _redis.GetEndPoints();
                    foreach (var ep in endpoints)
                    {
                        var server = _redis.GetServer(ep);
                        if (server.IsConnected)
                        {
                            var keys = server.Keys(pattern: $"{prefix}*").ToArray();
                            if (keys.Length > 0)
                            {
                                var db = _redis.GetDatabase();
                                await db.KeyDeleteAsync(keys);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to invalidate Redis leaderboard keys matching {Prefix}", prefix);
                }
            }
        }
    }
}

