using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using StudentOrganizationSystem.Models;

namespace StudentOrganizationSystem.Services
{
    /// <summary>
    /// Builds the <see cref="ClaimsPrincipal"/> the sign-in cookie carries. Shared
    /// by AccountController (on sign-in) and ProfileController (which re-issues the
    /// cookie after the profile picture changes), so the two can never disagree
    /// about which claims a signed-in user has.
    /// </summary>
    public static class UserClaims
    {
        public static ClaimsPrincipal Build(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.UserID.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    user.Username),

                new Claim(
                    ClaimTypes.Role,
                    user.Role?.RoleName ?? string.Empty),

                // C2: the stamp the cookie is re-checked against on every request.
                new Claim(
                    AppClaims.SecurityStamp,
                    user.SecurityStamp)
            };

            // C1: only a first-run administrator carries this, and it keeps every
            // other page closed until the password has been replaced.
            if (user.MustChangePassword)
            {
                claims.Add(new Claim(
                    AppClaims.MustChangePassword,
                    "true"));
            }

            // Only Member accounts point at a Members row (decision D3/D2).
            if (user.MemberID.HasValue)
            {
                claims.Add(new Claim(
                    AppClaims.MemberId,
                    user.MemberID.Value.ToString()));
            }

            if (!string.IsNullOrWhiteSpace(user.AvatarPath))
            {
                claims.Add(new Claim(
                    AppClaims.AvatarPath,
                    user.AvatarPath));
            }

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            return new ClaimsPrincipal(identity);
        }
    }
}
