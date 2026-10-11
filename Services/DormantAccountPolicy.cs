namespace StudentOrganizationSystem.Services
{
    /// <summary>
    /// C7: pure rule for deciding whether an account is dormant. Kept separate
    /// from the controller so it can be unit-tested without a database.
    /// </summary>
    public static class DormantAccountPolicy
    {
        /// <summary>
        /// Returns <c>true</c> when the account has not signed in within
        /// <paramref name="thresholdDays"/> days of <paramref name="now"/>.
        /// A <c>null</c> <paramref name="lastLoginAt"/> (never signed in) is
        /// treated as dormant.
        /// </summary>
        public static bool IsDormant(
            DateTime? lastLoginAt,
            int thresholdDays,
            DateTime now)
        {
            if (lastLoginAt == null) return true;
            return (now - lastLoginAt.Value).TotalDays >= thresholdDays;
        }
    }
}
