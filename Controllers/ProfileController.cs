using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Data;
using StudentOrganizationSystem.Models;
using StudentOrganizationSystem.Services;

namespace StudentOrganizationSystem.Controllers
{
    /// <summary>
    /// The signed-in user's own profile: view details and manage the profile
    /// picture. Any authenticated user (Admin, Officer or Member) can reach it;
    /// the id always comes from the sign-in cookie, never from the request, so a
    /// user can only ever edit their own row.
    /// </summary>
    [Authorize]
    public class ProfileController : Controller
    {
        /// <summary>Folder under wwwroot where uploaded pictures live.</summary>
        private const string AvatarFolder = "uploads/avatars";

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProfileController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: /Profile
        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUserAsync();

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View(user);
        }

        // POST: /Profile/UploadAvatar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile? avatar)
        {
            var user = await GetCurrentUserAsync();

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!AvatarUpload.IsAllowed(avatar, out var error))
            {
                TempData["Error"] = error;
                return RedirectToAction(nameof(Index));
            }

            var folder = AvatarFolderPath();
            Directory.CreateDirectory(folder);

            var fileName = AvatarUpload.BuildFileName(user.UserID, avatar!.FileName);
            var destination = Path.Combine(folder, fileName);

            await using (var stream = System.IO.File.Create(destination))
            {
                await avatar.CopyToAsync(stream);
            }

            // Drop the previous picture so the folder does not grow forever.
            DeleteAvatarFile(user.AvatarPath);

            user.AvatarPath = "/" + AvatarFolder + "/" + fileName;
            await _context.SaveChangesAsync();

            // Re-issue the cookie so the top bar shows the new picture at once.
            await RefreshSignInAsync(user);

            TempData["Success"] = "Your profile picture has been updated.";

            return RedirectToAction(nameof(Index));
        }

        // POST: /Profile/RemoveAvatar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAvatar()
        {
            var user = await GetCurrentUserAsync();

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            DeleteAvatarFile(user.AvatarPath);

            user.AvatarPath = null;
            await _context.SaveChangesAsync();

            await RefreshSignInAsync(user);

            TempData["Success"] = "Your profile picture has been removed.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(value, out var userId))
            {
                return null;
            }

            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Member)
                .FirstOrDefaultAsync(u => u.UserID == userId);
        }

        private string AvatarFolderPath()
        {
            var root = _environment.WebRootPath
                ?? Path.Combine(_environment.ContentRootPath, "wwwroot");

            return Path.Combine(root, AvatarFolder.Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// Deletes a stored picture, but only inside our own upload folder - the
        /// name is taken from the stored path and re-combined locally, so nothing
        /// outside the folder can be removed even if the value were tampered with.
        /// </summary>
        private void DeleteAvatarFile(string? avatarPath)
        {
            if (string.IsNullOrWhiteSpace(avatarPath))
            {
                return;
            }

            var fileName = Path.GetFileName(avatarPath);
            var fullPath = Path.Combine(AvatarFolderPath(), fileName);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }

        private async Task RefreshSignInAsync(User user)
        {
            var current = await HttpContext.AuthenticateAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            // Keep the "remember me" choice the user made at sign-in.
            var properties = current.Properties ?? new AuthenticationProperties();

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                UserClaims.Build(user),
                properties);
        }
    }
}
