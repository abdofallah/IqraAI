using System.Collections.Concurrent;

namespace IqraInfrastructure.Services
{
    /// <summary>
    /// Rate limiter for the tool test endpoint using sliding window counters.
    /// Enforces dual IP-based and email-based limits.
    /// </summary>
    public class ToolTestRateLimiter : IDisposable
    {
        private readonly ConcurrentDictionary<string, SlidingWindowCounter> _counters = new();
        private readonly int _ipLimitPerMinute;
        private readonly int _emailLimitPerMinute;
        private readonly Timer _cleanupTimer;

        public ToolTestRateLimiter(int ipLimitPerMinute = 10, int emailLimitPerMinute = 20)
        {
            _ipLimitPerMinute = ipLimitPerMinute;
            _emailLimitPerMinute = emailLimitPerMinute;

            // Cleanup expired counters every 5 minutes
            _cleanupTimer = new Timer(CleanupExpiredCounters, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// Check if the request is allowed for the given IP and email.
        /// Returns (isAllowed, reason) tuple.
        /// </summary>
        public (bool isAllowed, string? reason) CheckRateLimit(string ipAddress, string email)
        {
            string ipKey = $"ip:{ipAddress}";
            string emailKey = $"email:{email}";

            var ipCounter = _counters.GetOrAdd(ipKey, _ => new SlidingWindowCounter());
            if (!ipCounter.TryIncrement(_ipLimitPerMinute))
            {
                return (false, $"IP rate limit exceeded. Maximum {_ipLimitPerMinute} test requests per minute per IP address.");
            }

            var emailCounter = _counters.GetOrAdd(emailKey, _ => new SlidingWindowCounter());
            if (!emailCounter.TryIncrement(_emailLimitPerMinute))
            {
                // Rollback IP counter since we're rejecting
                ipCounter.Decrement();
                return (false, $"User rate limit exceeded. Maximum {_emailLimitPerMinute} test requests per minute per user.");
            }

            return (true, null);
        }

        private void CleanupExpiredCounters(object? state)
        {
            var now = DateTime.UtcNow;
            var keysToRemove = _counters
                .Where(kvp => kvp.Value.IsExpired(now))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _counters.TryRemove(key, out _);
            }
        }

        private class SlidingWindowCounter
        {
            private readonly Queue<DateTime> _timestamps = new();
            private readonly object _lock = new();
            private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

            public bool TryIncrement(int limit)
            {
                lock (_lock)
                {
                    var now = DateTime.UtcNow;
                    PurgeExpired(now);

                    if (_timestamps.Count >= limit)
                        return false;

                    _timestamps.Enqueue(now);
                    return true;
                }
            }

            public void Decrement()
            {
                lock (_lock)
                {
                    if (_timestamps.Count > 0)
                    {
                        // Remove the most recent entry (approximate rollback)
                        var list = _timestamps.ToList();
                        list.RemoveAt(list.Count - 1);
                        _timestamps.Clear();
                        foreach (var ts in list) _timestamps.Enqueue(ts);
                    }
                }
            }

            public bool IsExpired(DateTime now)
            {
                lock (_lock)
                {
                    PurgeExpired(now);
                    return _timestamps.Count == 0;
                }
            }

            private void PurgeExpired(DateTime now)
            {
                while (_timestamps.Count > 0 && now - _timestamps.Peek() > Window)
                {
                    _timestamps.Dequeue();
                }
            }
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }
    }
}
