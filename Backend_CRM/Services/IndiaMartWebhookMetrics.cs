using System.Collections.Concurrent;

namespace CRM.Services
{
    public sealed class IndiaMartWebhookMetricsSnapshot
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
        public DateTimeOffset? LastReceivedAtUtc { get; init; }
        public DateTimeOffset? LastSuccessAtUtc { get; init; }
        public DateTimeOffset? LastFailureAtUtc { get; init; }
        public DateTimeOffset CapturedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    }

    public interface IIndiaMartWebhookMetrics
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
        void RecordSuccess();
        void RecordFailure();
        IndiaMartWebhookMetricsSnapshot GetSnapshot();
    }

    /// <summary>
    /// Thread-safe process-local counters and timestamps for IndiaMART webhook observability.
    /// </summary>
    public sealed class IndiaMartWebhookMetrics : IIndiaMartWebhookMetrics
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
        private DateTimeOffset? _lastReceivedAtUtc;
        private DateTimeOffset? _lastSuccessAtUtc;
        private DateTimeOffset? _lastFailureAtUtc;
        private readonly object _timeLock = new();

        public void IncrementReceived()
        {
            Interlocked.Increment(ref _totalReceived);
            lock (_timeLock)
            {
                _lastReceivedAtUtc = DateTimeOffset.UtcNow;
            }
        }

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

        public void RecordSuccess()
        {
            lock (_timeLock)
            {
                _lastSuccessAtUtc = DateTimeOffset.UtcNow;
            }
        }

        public void RecordFailure()
        {
            lock (_timeLock)
            {
                _lastFailureAtUtc = DateTimeOffset.UtcNow;
            }
        }

        public IndiaMartWebhookMetricsSnapshot GetSnapshot()
        {
            DateTimeOffset? lastRec;
            DateTimeOffset? lastSucc;
            DateTimeOffset? lastFail;
            lock (_timeLock)
            {
                lastRec = _lastReceivedAtUtc;
                lastSucc = _lastSuccessAtUtc;
                lastFail = _lastFailureAtUtc;
            }

            return new IndiaMartWebhookMetricsSnapshot
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
                LastReceivedAtUtc = lastRec,
                LastSuccessAtUtc = lastSucc,
                LastFailureAtUtc = lastFail,
                CapturedAtUtc = DateTimeOffset.UtcNow
            };
        }
    }

    /// <summary>
    /// Process-local locks keyed by IndiaMART UNIQUE_QUERY_ID to serialize concurrent retries.
    /// </summary>
    public static class IndiaMartWebhookLeadLocks
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks =
            new(StringComparer.OrdinalIgnoreCase);

        public static async Task<IDisposable> AcquireAsync(string externalKey, CancellationToken cancellationToken)
        {
            var key = externalKey.Trim();
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
