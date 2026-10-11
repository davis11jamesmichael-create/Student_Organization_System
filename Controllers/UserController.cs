using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Data;
using StudentOrganizationSystem.Models;
using StudentOrganizationSystem.Services;

namespace StudentOrganizationSystem.Controllers
{
    // User and role management. Admin only - this is the screen that makes the
    // Admin/Officer/Member policy usable, because it is the only way to create an
    // account or approve a self-registered member.
    [Authorize(Roles = AppRoles.Admin)]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly SecurityStampGuard _stampValidator;

        public UserController(
            ApplicationDbContext context,
            IPasswordHasher<User> passwordHasher,
            SecurityStampGuard stampValidator)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _stampValidator = stampValidator;
        }

        // GET: /User
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .Include(u => u.Member)
                .OrderBy(u => u.Username)
                .ToListAsync();

            // Self-registered members land here inactive and wait for a decision.
            ViewBag.Pending = users
                .Where(u => !u.IsActive && u.MemberID.HasValue)
                .ToList();

            return View(users);
        }

        // GET: /User/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadLookupsAsync();

            return View(new UserFormViewModel());
        }

        // POST: /User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            UserFormViewModel model)
        {
            model.Username = model.Username.Trim();

            await LoadLookupsAsync();

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(
                    "Password",
                    "A password is required for a new account.");
            }

            await ValidateMemberLinkAsync(model);

            if (await _context.Users
                .AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError(
                    "Username",
                    "That username is already taken.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new User
            {
                Username = model.Username,
                RoleID = model.RoleID,
                MemberID = model.MemberID,
                IsActive = model.IsActive,
                CreatedAt = DateTime.Now
            };

            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                model.Password!);

            _context.Users.Add(user);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // The unique index on Users.Username / Users.MemberID is the real
                // guard (P1.4).
                ModelState.AddModelError(
                    "",
                    "That username is already taken, or the member already "
                        + "has an account.");

                return View(model);
            }

            TempData["Success"] =
                $"Account \"{user.Username}\" created.";

            return RedirectToAction(nameof(Index));
        }

        // GET: /User/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserID == id);

            if (user == null)
            {
                return NotFound();
            }

            await LoadLookupsAsync();

            return View(new UserFormViewModel
            {
                UserID = user.UserID,
                Username = user.Username,
                RoleID = user.RoleID,
                MemberID = user.MemberID,
                IsActive = user.IsActive
            });
        }

        // POST: /User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            UserFormViewModel model)
        {
            if (id != model.UserID)
            {
                return NotFound();
            }

            model.Username = model.Username.Trim();

            await LoadLookupsAsync();
            await ValidateMemberLinkAsync(model);

            var user = await _context.Users
                .FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            if (await _context.Users
                .AnyAsync(u => u.Username == model.Username
                            && u.UserID != id))
            {
                ModelState.AddModelError(
                    "Username",
                    "That username is already taken.");
            }

            // Never let the last way in be removed: the system would be locked
            // out of its own administration.
            if (IsRemovingLastAdmin(user, model.RoleID, model.IsActive))
            {
                ModelState.AddModelError(
                    "",
                    "This is the last active administrator. Promote another "
                        + "account first, then change this one.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // C2: what the account could do before the change, so its sign-in cookie is
            // only thrown away when it really must be.
            var sessionAffected = user.RoleID != model.RoleID
                || user.IsActive != model.IsActive
                || !string.IsNullOrWhiteSpace(model.Password);

            user.Username = model.Username;
            user.RoleID = model.RoleID;
            user.MemberID = model.MemberID;
            user.IsActive = model.IsActive;

            // A blank password box means "keep the current password".
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                user.PasswordHash = _passwordHasher.HashPassword(
                    user,
                    model.Password);
            }

            // C2: a new role, a deactivation or a new password ends every session
            // the account already had; renaming it alone does not.
            InvalidateSessions(user, sessionAffected);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    "",
                    "That username is already taken, or the member already "
                        + "has an account.");

                return View(model);
            }

            TempData["Success"] =
                $"Account \"{user.Username}\" updated.";

            return RedirectToAction(nameof(Index));
        }

        // POST: /User/Approve/5
        // A self-registered member: activating the account also activates the
        // Members row it is linked to, so the pair can never disagree.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var user = await _context.Users
                .Include(u => u.Member!)
                    .ThenInclude(m => m.Memberships)
                .FirstOrDefaultAsync(u => u.UserID == id);

            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = true;

            if (user.Member != null)
            {
                user.Member.IsActive = true;
                user.Member.UpdatedAt = DateTime.Now;

                // Decision D2: approving the account also puts the membership
                // back to "Active" (never leave a live member "Inactive").
                MembershipPolicy.SyncStanding(user.Member);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Registration for \"{user.Username}\" approved.";

            return RedirectToAction(nameof(Index));
        }

        // POST: /User/Reject/5
        // Removes a pending registration. Refused once the member already has
        // history, because deleting it would silently destroy records.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == id);

            if (user == null)
            {
                return NotFound();
            }

            if (!user.MemberID.HasValue)
            {
                TempData["Error"] =
                    "Only a pending member registration can be rejected.";

                return RedirectToAction(nameof(Index));
            }

            var memberId = user.MemberID.Value;

            var hasHistory =
                await _context.Attendances.AnyAsync(a => a.MemberID == memberId) ||
                await _context.Participations.AnyAsync(p => p.MemberID == memberId) ||
                await _context.Memberships.AnyAsync(m => m.MemberID == memberId);

            if (hasHistory)
            {
                TempData["Error"] =
                    "This member already has records, so the registration "
                        + "cannot be deleted. Deactivate the account instead.";

                return RedirectToAction(nameof(Index));
            }

            // The Users row owns the link, so detach it before removing the pair
            // or the Restrict foreign key would block the delete.
            user.MemberID = null;
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            var member = await _context.Members
                .FindAsync(memberId);

            if (member != null)
            {
                _context.Members.Remove(member);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] =
                "The pending registration was rejected and removed.";

            return RedirectToAction(nameof(Index));
        }

        // POST: /User/Deactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var user = await _context.Users
                .FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            if (user.UserID == GetCurrentUserId())
            {
                TempData["Error"] =
                    "You cannot deactivate the account you are signed in with.";

                return RedirectToAction(nameof(Index));
            }

            if (IsRemovingLastAdmin(user, user.RoleID, false))
            {
                TempData["Error"] =
                    "This is the last active administrator and cannot be "
                        + "deactivated.";

                return RedirectToAction(nameof(Index));
            }

            user.IsActive = false;

            // C2: the deactivated account loses its session at once instead of
            // keeping it until the cookie expires.
            InvalidateSessions(user, true);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Account \"{user.Username}\" deactivated.";

            return RedirectToAction(nameof(Index));
        }

        // POST: /User/Activate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var user = await _context.Users
                .FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = true;

            // C2: the account starts again with a fresh stamp, so no cookie minted
            // before the deactivation can come back to life.
            InvalidateSessions(user, true);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Account \"{user.Username}\" activated.";

            return RedirectToAction(nameof(Index));
        }

        // POST: /User/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            int id,
            string newPassword)
        {
            var user = await _context.Users
                .FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(newPassword)
                || newPassword.Length < 6)
            {
                TempData["Error"] =
                    "The new password must be at least 6 characters.";

                return RedirectToAction(nameof(Edit), new { id });
            }

            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                newPassword);

            // C2: an administrative reset ends whatever session the account had.
            InvalidateSessions(user, true);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Password for \"{user.Username}\" was reset.";

            return RedirectToAction(nameof(Edit), new { id });
        }

        /// <summary>
        /// A Member account must point at a Members row that has no other account,
        /// or the login has nothing to show and the unique index would fail.
        /// Admin and Officer accounts carry no link.
        /// </summary>
        private async Task ValidateMemberLinkAsync(UserFormViewModel model)
        {
            var memberRoleId = await GetRoleIdAsync(AppRoles.Member);

            if (model.RoleID != memberRoleId)
            {
                // Staff accounts never hold a member link.
                model.MemberID = null;

                return;
            }

            if (!model.MemberID.HasValue)
            {
                ModelState.AddModelError(
                    "MemberID",
                    "Choose the member this account belongs to.");

                return;
            }

            var memberExists = await _context.Members
                .AnyAsync(m => m.MemberID == model.MemberID.Value);

            if (!memberExists)
            {
                ModelState.AddModelError(
                    "MemberID",
                    "That member no longer exists.");

                return;
            }

            var alreadyLinked = await _context.Users
                .AnyAsync(u => u.MemberID == model.MemberID.Value
                            && u.UserID != model.UserID);

            if (alreadyLinked)
            {
                ModelState.AddModelError(
                    "MemberID",
                    "That member already has a login account.");
            }
        }

        /// <summary>
        /// True when the change would leave the system with no active
        /// administrator. The seeded admin is the only way in, so this is the one
        /// mistake that would be unrecoverable.
        /// </summary>
        private bool IsRemovingLastAdmin(
            User user,
            int newRoleId,
            bool newIsActive)
        {
            var adminRoleId = _context.Roles
                .AsNoTracking()
                .FirstOrDefault(r => r.RoleName == AppRoles.Admin)
                ?.RoleID;

            if (adminRoleId == null || user.RoleID != adminRoleId)
            {
                return false;
            }

            var stillAdmin = newRoleId == adminRoleId && newIsActive;

            if (stillAdmin)
            {
                return false;
            }

            // Counted without the row being changed, so it is still "active".
            var otherAdmins = _context.Users
                .AsNoTracking()
                .Count(u => u.RoleID == adminRoleId
                         && u.IsActive
                         && u.UserID != user.UserID);

            return otherAdmins == 0 && user.IsActive;
        }

        private async Task LoadLookupsAsync()
        {
            ViewBag.Roles = await _context.Roles
                .AsNoTracking()
                .OrderBy(r => r.RoleID)
                .ToListAsync();

            // Only members without an account can be linked.
            var linkedIds = await _context.Users
                .AsNoTracking()
                .Where(u => u.MemberID.HasValue)
                .Select(u => u.MemberID!.Value)
                .ToListAsync();

            var available = await _context.Members
                .AsNoTracking()
                .Where(m => !linkedIds.Contains(m.MemberID))
                .OrderBy(m => m.FullName)
                .ToListAsync();

            ViewBag.MemberOptions = new SelectList(
                available,
                "MemberID",
                "FullName");
        }

        private async Task<int> GetRoleIdAsync(string roleName)
        {
            var role = await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoleName == roleName);

            return role?.RoleID ?? 0;
        }

        /// <summary>
        /// C2: ends every sign-in cookie an account already holds by giving it a
        /// new security stamp, and drops the cached copy so the very next request
        /// already sees it. <paramref name="changed"/> is false for an edit that
        /// leaves the account's powers and password alone (a rename).
        /// </summary>
        private void InvalidateSessions(User user, bool changed)
        {
            if (!changed)
            {
                return;
            }

            user.SecurityStamp = SecurityStamps.New();
            _stampValidator.Evict(user.UserID);
        }

        private int GetCurrentUserId()
        {
            var value = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(value, out var id) ? id : 0;
        }
    }
}