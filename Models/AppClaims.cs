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
    }
}