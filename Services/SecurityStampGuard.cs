using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StudentOrganizationSystem.Data;

namespace StudentOrganizationSystem.Services
{
    /// <summary>
    /// The live values one sign-in cookie is checked against: the current security
    /// stamp, whether the account is still active, and the role it holds now.
    /// </summary>
    public sealed record UserSnapshot(
        string? SecurityStamp,
        bool IsActive,
        string? RoleName);

    /// <summary>
    /// C2 (`boltai.md` finding 2): re-checks the sign-in cookie against the Users
    /// row on every request, so a role change, a deactivation or a password reset
    /// ends the old session instead of staying valid until the cookie expires.
    ///
    /// The lookup is cached for a short window so a busy page does not become a
    /// database round trip per request, and every action that changes an account
    /// evicts the entry itself, so its own change takes effect immediately.
    ///
    /// (Named "Guard" rather than "Validator" because
    /// <c>Microsoft.AspNetCore.Identity.SecurityStampValidator</c> already owns
    /// the obvious name.)
    /// </summary>
    public class SecurityStampGuard
    {
        /// <summary>How long one account's current values are remembered.</summary>
        public static readonly TimeSpan CacheWindow = TimeSpan.FromMinutes(1);

        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public SecurityStampGuard(
            ApplicationDbContext context,
            IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public static string BuildKey(int userId) => $"security-stamp:{userId}";

        /// <summary>
        /// The rule, kept pure so it is unit-tested without a database or a
        /// request: the cookie is still good only when it holds exactly the stamp,
        /// role and active flag the database has right now. A missing stamp on
        /// either side counts as stale, so a cookie minted before this check
        /// existed cannot be reused.
        /// </summary>
        public static bool IsCurrent(
            string? cookieStamp,
            string? currentStamp,
            bool isActive,
            string? cookieRole,
            string? currentRole)
        {
            return isActive
                && !string.IsNullOrEmpty(cookieStamp)
                && string.Equals(
                    cookieStamp,
                    currentStamp,
                    StringComparison.Ordinal)
                && string.Equals(
                    cookieRole ?? string.Empty,
                    currentRole ?? string.Empty,
                    StringComparison.Ordinal);
        }

        /// <summary>
        /// The account's current values, or null when the row is gone (a rejected
        /// registration). Cached briefly - see <see cref="CacheWindow"/>.
        /// </summary>
        public async Task<UserSnapshot?> GetCurrentAsync(int userId)
        {
            var key = BuildKey(userId);

            if (_cache.TryGetValue(key, out UserSnapshot? cached))
            {
                return cached;
            }

            var snapshot = await _context.Users
                .AsNoTracking()
                .Where(u => u.UserID == userId)
                .Select(u => new UserSnapshot(
                    u.SecurityStamp,
                    u.IsActive,
                    u.Role != null ? u.Role.RoleName : string.Empty))
                .FirstOrDefaultAsync();

            _cache.Set(key, snapshot, CacheWindow);

            return snapshot;
        }

        /// <summary>
        /// Drops the cached copy for one account. Called by the actions that change
        /// a stamp, so the very next request already sees the new value.
        /// </summary>
        public void Evict(int userId) => _cache.Remove(BuildKey(userId));
    }
}