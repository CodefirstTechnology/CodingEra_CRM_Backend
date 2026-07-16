using System.Collections.Concurrent;

namespace CRM.Services
{
    public sealed class JustdialWebhookMetricsSnapshot
    {
        public long TotalReceived { get; init; }
        public long Inserted { get; init; }
        public long Duplicates { get; init; }
        public long ValidationFailed { get; init; }
        public long PersistenceFailed { get; init; }
        public long AuthFailed { get; init; }
        public long SkippedDisabled { get; init; }
        public long Malformed { get; init; }
        public long TotalProcessingTimeMs { get; init; }
        public long SampleCount { get; init; }
        public double AverageProcessingTimeMs =>
            SampleCount == 0 ? 0 : (double)TotalProcessingTimeMs / SampleCount;
        public DateTimeOffset CapturedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    }

    public interface IJustdialWebhookMetrics
    {
        void IncrementReceived();
        void IncrementInserted();
        void IncrementDuplicates();
        void IncrementValidationFailed();
        void IncrementPersistenceFailed();
        void IncrementAuthFailed();
        void IncrementSkippedDisabled();
        void IncrementMalformed();
        void RecordProcessingTime(long elapsedMs);
        JustdialWebhookMetricsSnapshot GetSnapshot();
    }

    /// <summary>
    /// Process-local counters for Justdial webhook observability.
    /// Suitable for single-node diagnostics; replace with Prometheus/OpenTelemetry for multi-node production.
    /// </summary>
    public sealed class JustdialWebhookMetrics : IJustdialWebhookMetrics
    {
        private long _totalReceived;
        private long _inserted;
        private long _duplicates;
        private long _validationFailed;
        private long _persistenceFailed;
        private long _authFailed;
        private long _skippedDisabled;
        private long _malformed;
        private long _totalProcessingTimeMs;
        private long _sampleCount;

        public void IncrementReceived() => Interlocked.Increment(ref _totalReceived);
        public void IncrementInserted() => Interlocked.Increment(ref _inserted);
        public void IncrementDuplicates() => Interlocked.Increment(ref _duplicates);
        public void IncrementValidationFailed() => Interlocked.Increment(ref _validationFailed);
        public void IncrementPersistenceFailed() => Interlocked.Increment(ref _persistenceFailed);
        public void IncrementAuthFailed() => Interlocked.Increment(ref _authFailed);
        public void IncrementSkippedDisabled() => Interlocked.Increment(ref _skippedDisabled);
        public void IncrementMalformed() => Interlocked.Increment(ref _malformed);

        public void RecordProcessingTime(long elapsedMs)
        {
            Interlocked.Add(ref _totalProcessingTimeMs, Math.Max(0, elapsedMs));
            Interlocked.Increment(ref _sampleCount);
        }

        public JustdialWebhookMetricsSnapshot GetSnapshot() =>
            new()
            {
                TotalReceived = Interlocked.Read(ref _totalReceived),
                Inserted = Interlocked.Read(ref _inserted),
                Duplicates = Interlocked.Read(ref _duplicates),
                ValidationFailed = Interlocked.Read(ref _validationFailed),
                PersistenceFailed = Interlocked.Read(ref _persistenceFailed),
                AuthFailed = Interlocked.Read(ref _authFailed),
                SkippedDisabled = Interlocked.Read(ref _skippedDisabled),
                Malformed = Interlocked.Read(ref _malformed),
                TotalProcessingTimeMs = Interlocked.Read(ref _totalProcessingTimeMs),
                SampleCount = Interlocked.Read(ref _sampleCount),
                CapturedAtUtc = DateTimeOffset.UtcNow
            };
    }

    /// <summary>
    /// Process-local locks keyed by Justdial leadid to reduce duplicate inserts under concurrent retries.
    /// </summary>
    public static class JustdialWebhookLeadLocks
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks =
            new(StringComparer.OrdinalIgnoreCase);

        public static async Task<IDisposable> AcquireAsync(string leadId, CancellationToken cancellationToken)
        {
            var key = leadId.Trim();
            var gate = Locks.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(cancellationToken);
            return new Releaser(gate);
        }

        private sealed class Releaser : IDisposable
        {
            private readonly SemaphoreSlim _gate;
            private bool _disposed;

            public Releaser(SemaphoreSlim gate)
            {
                _gate = gate;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _gate.Release();
            }
        }
    }
}
