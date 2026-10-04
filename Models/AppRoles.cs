namespace StudentOrganizationSystem.Models
{
    /// <summary>
    /// The role names that exist in the Roles table and that are written into the
    /// sign-in cookie by AccountController. Shared constants, so the seeded data,
    /// the controllers and the views can never drift apart (AUDIT.md P0.6 / D3).
    /// </summary>
    public static class AppRoles
    {
        /// <summary>
        /// Full control: manages accounts, members, activities and every status
        /// change (activating and deactivating records).
        /// </summary>
        public const string Admin = "Admin";

        /// <summary>
        /// Day-to-day work: view records, register members and activities, and
        /// record attendance. Cannot change or deactivate an existing record.
        /// </summary>
        public const string Officer = "Officer";

        /// <summary>
        /// A student who signed up through the public registration page and was
        /// approved by an Admin. Can only read their own records through the
        /// Portal; every management screen is off limits. The account is linked
        /// to exactly one Members row through Users.MemberID.
        /// </summary>
        public const string Member = "Member";

        /// <summary>
        /// The two staff roles. Used as one string so a controller can say
        /// "staff only" without listing the roles twice and risking a typo.
        /// Const concatenation happens at compile time, so this is valid inside
        /// [Authorize(Roles = ...)].
        /// </summary>
        public const string AdminOrOfficer = Admin + "," + Officer;
    }
}
