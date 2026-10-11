using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Data;
using StudentOrganizationSystem.Models;
using StudentOrganizationSystem.Services;

namespace StudentOrganizationSystem.Controllers
{
    // Read (Index, Details): any authenticated user — staff and Members both see
    // announcements. Write (Create, Edit, Deactivate, Activate): staff only.
    [Authorize]
    public class AnnouncementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notifications;

        public AnnouncementController(
            ApplicationDbContext context,
            NotificationService notifications)
        {
            _context = context;
            _notifications = notifications;
        }

        // GET: /Announcement
        public async Task<IActionResult> Index(string? search, string? audience, int page = 1)
        {
            var query = _context.Announcements
                .AsNoTracking()
                .Where(a => a.IsActive);

            // Audience filter: Members see only All + Members; Staff see all.
            if (User.IsInRole(AppRoles.Member))
            {
                query = query.Where(a =>
                    a.Audience == AnnouncementAudience.All ||
                    a.Audience == AnnouncementAudience.Members);
            }
            else if (audience != null && AnnouncementAudience.IsValid(audience))
            {
                query = query.Where(a => a.Audience == audience);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(a => a.Title.Contains(search) || a.Body.Contains(search));
            }

            // Pinned first, then newest.
            query = query
                .OrderByDescending(a => a.IsPinned)
                .ThenByDescending(a => a.CreatedAt);

            var paged = await Paging.ToPagedAsync(query, page);

            ViewBag.Search = search;
            ViewBag.Audience = audience;
            ViewBag.IsAdmin = User.IsInRole(AppRoles.Admin);
            ViewBag.IsStaff = User.IsInRole(AppRoles.AdminOrOfficer);
            ViewBag.Pager = paged.ToPager(
                "Announcement",
                "Index",
                new Dictionary<string, string>
                {
                    ["search"] = search ?? string.Empty,
                    ["audience"] = audience ?? string.Empty
                });

            return View(paged.Items);
        }

        // GET: /Announcement/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var announcement = await _context.Announcements
                .AsNoTracking()
                .Include(a => a.CreatedByUser)
                .FirstOrDefaultAsync(a => a.AnnouncementID == id && a.IsActive);

            if (announcement == null) return NotFound();

            // Members may only see All or Members audience.
            if (User.IsInRole(AppRoles.Member) &&
                announcement.Audience == AnnouncementAudience.Staff)
            {
                return Forbid();
            }

            ViewBag.IsStaff = User.IsInRole(AppRoles.AdminOrOfficer);

            return View(announcement);
        }

        // GET: /Announcement/Create
        [Authorize(Roles = AppRoles.AdminOrOfficer)]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Announcement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdminOrOfficer)]
        public async Task<IActionResult> Create(
            [Bind("Title,Body,Audience,IsPinned")] Announcement announcement)
        {
            announcement.Title = (announcement.Title ?? string.Empty).Trim();
            announcement.Body = (announcement.Body ?? string.Empty).Trim();

            if (!AnnouncementAudience.IsValid(announcement.Audience))
            {
                ModelState.AddModelError("Audience", "Choose a valid audience.");
            }

            ModelState.Clear();
            TryValidateModel(announcement);

            if (!ModelState.IsValid) return View(announcement);

            announcement.IsActive = true;
            announcement.CreatedAt = DateTime.Now;
            announcement.CreatedByUserID = GetCurrentUserId();

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            // B2: notify eligible users about the new announcement.
            await _notifications.NotifyAnnouncementAsync(
                announcement.AnnouncementID,
                announcement.Title,
                announcement.Audience,
                GetCurrentUserId());

            TempData["Success"] = "Announcement posted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Announcement/Edit/5
        [Authorize(Roles = AppRoles.AdminOrOfficer)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null) return NotFound();

            return View(announcement);
        }

        // POST: /Announcement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdminOrOfficer)]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("AnnouncementID,Title,Body,Audience,IsPinned,IsActive")] Announcement announcement)
        {
            if (id != announcement.AnnouncementID) return NotFound();

            announcement.Title = (announcement.Title ?? string.Empty).Trim();
            announcement.Body = (announcement.Body ?? string.Empty).Trim();

            if (!AnnouncementAudience.IsValid(announcement.Audience))
            {
                ModelState.AddModelError("Audience", "Choose a valid audience.");
            }

            ModelState.Clear();
            TryValidateModel(announcement);

            if (!ModelState.IsValid) return View(announcement);

            var existing = await _context.Announcements.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Title = announcement.Title;
            existing.Body = announcement.Body;
            existing.Audience = announcement.Audience;
            existing.IsPinned = announcement.IsPinned;
            existing.IsActive = announcement.IsActive;
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Announcement updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Announcement/Deactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdminOrOfficer)]
        public async Task<IActionResult> Deactivate(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null) return NotFound();

            announcement.IsActive = false;
            announcement.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Announcement deactivated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Announcement/Activate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdminOrOfficer)]
        public async Task<IActionResult> Activate(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null) return NotFound();

            announcement.IsActive = true;
            announcement.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Announcement activated.";
            return RedirectToAction(nameof(Index));
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }
    }
}
