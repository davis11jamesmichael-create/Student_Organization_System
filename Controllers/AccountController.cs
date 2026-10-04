using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Data;
using StudentOrganizationSystem.Models;
using StudentOrganizationSystem.Services;

namespace StudentOrganizationSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly LoginAttemptTracker _attemptTracker;

        public AccountController(
            ApplicationDbContext context,
            IPasswordHasher<User> passwordHasher,
            LoginAttemptTracker attemptTracker)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _attemptTracker = attemptTracker;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Only a local path is kept (P1.9), so it is safe to hand to the view.
            returnUrl = CleanReturnUrl(returnUrl);

            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToHomeFor(User, returnUrl);
            }

            ViewData["ReturnUrl"] = returnUrl;

            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            string? returnUrl = null)
        {
            // Re-clean the posted value; never trust what the form sent back.
            returnUrl = CleanReturnUrl(returnUrl);
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // P1.9: count failures for this username + address pair.
            var attemptKey = LoginAttemptTracker.BuildKey(
                model.Username,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            if (_attemptTracker.IsLockedOut(attemptKey))
            {
                ModelState.AddModelError(
                    "",
                    "Too many failed log-in attempts. Please wait a few "
                        + "minutes before trying again.");

                return View(model);
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(
                    u => u.Username == model.Username);

            if (user == null)
            {
                _attemptTracker.RecordFailure(attemptKey);

                ModelState.AddModelError(
                    "",
                    "Invalid username or password.");

                return View(model);
            }

            // Deliberately not part of the query above. A self-registered member
            // whose account is still pending must be told so, instead of getting
            // the generic message and thinking their registration was lost.
            if (!user.IsActive)
            {
                ModelState.AddModelError(
                    "",
                    "This account is awaiting administrator approval, "
                        + "or has been deactivated. Please contact the "
                        + "organization adviser.");

                return View(model);
            }

            var passwordResult =
                _passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    model.Password);

            if (passwordResult ==
                PasswordVerificationResult.Failed)
            {
                _attemptTracker.RecordFailure(attemptKey);

                ModelState.AddModelError(
                    "",
                    "Invalid username or password.");

                return View(model);
            }

            // P1.9: a hash written by an older algorithm reports
            // SuccessRehashNeeded. Re-hash with the current algorithm while the
            // plain password is still in hand, so old accounts upgrade silently.
            if (passwordResult ==
                PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash =
                    _passwordHasher.HashPassword(
                        user,
                        model.Password);

                await _context.SaveChangesAsync();
            }

            // P1.9: a successful sign-in clears the failure counter for this
            // username + address pair.
            _attemptTracker.Reset(attemptKey);

            // The claim set (identity, role, member link and avatar) is built in
            // one place so ProfileController can re-issue the same cookie after an
            // avatar change.
            var principal = UserClaims.Build(user);

            var authenticationProperties =
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe
                };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authenticationProperties);

            return RedirectToHomeFor(principal, returnUrl);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction(
                "Login",
                "Account");
        }

        // GET: /Account/Register
        // Public self-registration for students. The Members row and the login are
        // both created inactive; an Admin approves them on the Users screen.
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToHomeFor(User);
            }

            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // P1.6: trim before validating and saving. MySQL 8's default
            // collation is NO PAD, so "2024-00001 " and "2024-00001" would
            // otherwise both pass the unique index as two different values.
            model.Username = model.Username.Trim();
            model.StudentID = model.StudentID.Trim();
            model.FullName = model.FullName.Trim();
            model.Course = model.Course.Trim();
            model.YearLevel = model.YearLevel.Trim();

            if (await _context.Users
                .AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError(
                    "Username",
                    "That username is already taken.");

                return View(model);
            }

            if (await _context.Members
                .AnyAsync(m => m.StudentID == model.StudentID))
            {
                ModelState.AddModelError(
                    "StudentID",
                    "This Student ID is already registered. If the "
                        + "account is yours, please contact the adviser.");

                return View(model);
            }

            var memberRole = await _context.Roles
                .FirstOrDefaultAsync(
                    r => r.RoleName == AppRoles.Member);

            if (memberRole == null)
            {
                ModelState.AddModelError(
                    "",
                    "Self-registration is not available right now.");

                return View(model);
            }

            // Both rows start inactive. Approving the account activates the pair
            // (UserController.Approve), so a pending registration can never reach
            // a staff screen even if it finds its way to a protected URL.
            var member = new Member
            {
                StudentID = model.StudentID,
                FullName = model.FullName,
                Course = model.Course,
                YearLevel = model.YearLevel,
                IsActive = false,
                CreatedAt = DateTime.Now
            };

            // Decision D2: the pending member gets a matching membership row
            // (inactive), so approving the account can simply activate both.
            MembershipPolicy.Ensure(member);

            var account = new User
            {
                Username = model.Username,
                RoleID = memberRole.RoleID,
                IsActive = false,
                CreatedAt = DateTime.Now,
                Member = member
            };

            account.PasswordHash =
                _passwordHasher.HashPassword(
                    account,
                    model.Password);

            _context.Users.Add(account);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // The unique indexes on Users.Username and Members.StudentID are
                // the real guard: two overlapping submissions can slip past the
                // pre-checks above (P1.4).
                ModelState.AddModelError(
                    "",
                    "That username or Student ID was registered a moment "
                        + "ago. Please check the form and try again.");

                return View(model);
            }

            return RedirectToAction(nameof(RegisterPending));
        }

        // GET: /Account/RegisterPending
        [HttpGet]
        [AllowAnonymous]
        public IActionResult RegisterPending()
        {
            return View();
        }

        // GET: /Account/ChangePassword
        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userId, out var id))
            {
                return RedirectToAction(nameof(Login));
            }

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                model.CurrentPassword);

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    "CurrentPassword",
                    "Your current password is not correct.");

                return View(model);
            }

            // The plain new password is hashed here; it is never stored.
            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                model.NewPassword);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Your password has been changed.";

            return RedirectToHomeFor(User);
        }

        // Where a signed-in user belongs after signing in. A Member has no access
        // to the staff Dashboard, so sending them there would only bounce them
        // straight back to /Account/AccessDenied.
        //
        // P1.9: when the user was bounced off a protected page, returnUrl holds
        // that page and it wins over the dashboard default - but only after
        // CleanReturnUrl has proved it is a safe local path, so the login page can
        // never be turned into an open redirect.
        private IActionResult RedirectToHomeFor(
            ClaimsPrincipal principal,
            string? returnUrl = null)
        {
            if (principal.IsInRole(AppRoles.Member))
            {
                return RedirectToAction("Index", "Portal");
            }

            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// P1.9: only a local path may survive the sign-in round trip. Absolute
        /// URLs and the scheme-relative "//host" form are dropped, so the login
        /// page cannot be used to bounce a user to another site.
        /// </summary>
        private string? CleanReturnUrl(string? returnUrl)
        {
            return !string.IsNullOrWhiteSpace(returnUrl)
                && Url.IsLocalUrl(returnUrl)
                    ? returnUrl
                    : null;
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}