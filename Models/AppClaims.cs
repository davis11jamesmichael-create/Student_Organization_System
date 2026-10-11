namespace StudentOrganizationSystem.Models
{
    /// <summary>
    /// Custom claim types that the sign-in cookie carries on top of the standard
    /// ones. Kept in one place so AccountController (which writes the claim) and
    /// PortalController (which reads it) can never disagree about the name.
    /// </summary>
    public static class AppClaims
    {
        /// <summary>
        /// The Members.MemberID that a "Member" login is linked to. Written into
        /// the cookie only for Member accounts, so PortalController can scope every
        /// query to "my own records" without trusting anything from the request.
        /// </summary>
        public const string MemberId = "MemberID";

        /// <summary>
        /// The web-root-relative path to the user's profile picture, when one has
        /// been uploaded. Written into the cookie so the top bar can show the
        /// picture without a database round trip on every page.
        /// </summary>
        public const string AvatarPath = "AvatarPath";

        /// <summary>
        /// The user's <c>SecurityStamp</c> at sign-in time. C2: Program.cs compares
        /// it with the stored value on every request, so a role change or a
        /// deactivation rejects the cookie instead of honoring it for eight hours.
        /// </summary>
        public const string SecurityStamp = "SecurityStamp";

        /// <summary>
        /// Present (with the value "true") only while the account must still
        /// replace the password it was given. C1: a first-run administrator can
        /// then reach nothing but the change-password form.
        /// </summary>
        public const string MustChangePassword = "MustChangePassword";
    }
}