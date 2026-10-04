using Microsoft.Extensions.Caching.Memory;

namespace StudentOrganizationSystem.Services
{
    /// <summary>
    /// A deliberately small brute-force guard for the sign-in form (AUDIT.md
    /// P1.9). It counts failures per username + IP address in <see cref="IMemoryCache"/>
    /// and locks the pair out for a few minutes once the limit is reached.
    ///
    /// It is process-local on purpose: the project has no distributed cache and
    /// one web server, so a cache-backed counter is the honest, dependency-free
    /// answer. Restarting the app clears the counters, which is acceptable here.
    /// </summary>
    public class LoginAttemptTracker
    {
        /// <summary>Failures allowed before the key is locked out.</summary>
        public const int MaxAttempts = 5;

        /// <summary>How long a failure count (and a lockout) is remembered.</summary>
        public static readonly TimeSpan Window = TimeSpan.FromMinutes(5);

        private readonly IMemoryCache _cache;

        public LoginAttemptTracker(IMemoryCache cache)
        {
            _cache = cache;
        }

        /// <summary>
        /// Builds the cache key for one username + client address, so a single
        /// attacker cannot lock out an unrelated user from another address.
        /// </summary>
        public static string BuildKey(string? username, string? ipAddress)
        {
            var name = (username ?? string.Empty)
                .Trim()
                .ToLowerInvariant();

            return $"login-attempt:{name}:{ipAddress ?? "unknown"}";
        }

        public bool IsLockedOut(string key)
        {
            return _cache.TryGetValue(key, out int count)
                && count >= MaxAttempts;
        }

        /// <summary>Records one failure and returns the running total.</summary>
        public int RecordFailure(string key)
        {
            var count = _cache.TryGetValue(key, out int existing)
                ? existing + 1
                : 1;

            // Sliding window: every failure restarts the clock, so a slow
            // guesser is stopped just as effectively as a fast one.
            _cache.Set(key, count, Window);

            return count;
        }

        /// <summary>Clears the counter after a successful sign-in.</summary>
        public void Reset(string key)
        {
            _cache.Remove(key);
        }
    }
}
