namespace StudentOrganizationSystem.Models
{
    /// <summary>
    /// The value stored in <see cref="User.SecurityStamp"/> and carried in the
    /// sign-in cookie. Changing the stored value invalidates every cookie that
    /// still holds the old one, which is how a role change, a deactivation or a
    /// password reset ends one session instead of leaving it usable for the rest
    /// of the eight-hour window (`boltai.md` finding 2 / C2).
    /// </summary>
    public static class SecurityStamps
    {
        /// <summary>A new, unguessable stamp (32 hex characters).</summary>
        public static string New() => Guid.NewGuid().ToString("N");
    }
}