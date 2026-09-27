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
    }
}
