using Microsoft.AspNetCore.Http;

namespace StudentOrganizationSystem.Services
{
    /// <summary>
    /// C1 (`boltai.md` finding 1): until the first-run administrator has replaced
    /// the password the seeder printed once, the only pages that may open are the
    /// change-password form, the sign-out form and static files. Everything else
    /// redirects back to the form, so a password read off a console log cannot be
    /// used to browse the system. Pure, so the rule is unit-tested directly.
    /// </summary>
    public static class MustChangePasswordPolicy
    {
        private static readonly string[] AllowedPrefixes =
        {
            "/Account/ChangePassword",
            "/Account/Logout",
            "/Account/Login",
            "/Account/AccessDenied",
            "/Home/Error",
            "/css",
            "/js",
            "/lib",
            "/uploads",
            "/favicon.ico"
        };

        /// <summary>
        /// True when a signed-in user who must still change their password may
        /// open this path. Segment-aware, so "/Account/Logout" is allowed while
        /// "/Account/LogoutSomething" is not.
        /// </summary>
        public static bool IsAllowed(PathString path)
        {
            foreach (var prefix in AllowedPrefixes)
            {
                if (path.StartsWithSegments(
                    prefix,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}