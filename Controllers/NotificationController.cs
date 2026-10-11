using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Data;
using StudentOrganizationSystem.Models;

namespace StudentOrganizationSystem.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Notification
        public async Task<IActionResult> Index(int page = 1)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Challenge();

            var query = _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserID == userId)
                .OrderBy(n => n.IsRead)
                .ThenByDescending(n => n.CreatedAt);

            var paged = await Paging.ToPagedAsync(query, page);

            ViewBag.Pager = paged.ToPager(
                "Notification",
                "Index",
                new Dictionary<string, string>());

            ViewBag.UnreadCount = paged.Items.Count(n => !n.IsRead);

            return View(paged.Items);
        }

        // POST: /Notification/MarkRead/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Challenge();

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationID == id && n.UserID == userId);

            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Notification/MarkAllRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Challenge();

            var unread = await _context.Notifications
                .Where(n => n.UserID == userId && !n.IsRead)
                .ToListAsync();

            if (unread.Count > 0)
            {
                var now = DateTime.Now;
                foreach (var n in unread)
                {
                    n.IsRead = true;
                    n.ReadAt = now;
                }
                await _context.SaveChangesAsync();
                TempData["Success"] = "All notifications marked as read.";
            }

            return RedirectToAction(nameof(Index));
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }
    }
}
