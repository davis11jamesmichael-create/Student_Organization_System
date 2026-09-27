using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Data;
using StudentOrganizationSystem.Models;

namespace StudentOrganizationSystem.Controllers
{
    // Role policy (AUDIT.md P0.6, decision D3):
    //   any signed-in user -> Index, Details, adding a new activity, and recording
    //                         attendance (that is the Officer's day-to-day work);
    //   Admin only         -> changing or (de)activating an existing activity.
    [Authorize]
    public class ActivityController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ActivityController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Activity
        public async Task<IActionResult> Index(
            string? search,
            DateTime? activityDate,
            string? status)
        {
            var query = _context.Activities.AsQueryable();

            // Search by activity title
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(a =>
                    a.ActivityTitle.Contains(search));
            }

            // Search by exact activity date
            if (activityDate.HasValue)
            {
                query = query.Where(a =>
                    a.ActivityDate.Date == activityDate.Value.Date);
            }

            // Filter Active / Inactive
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status == "Active")
                {
                    query = query.Where(a => a.IsActive);
                }
                else if (status == "Inactive")
                {
                    query = query.Where(a => !a.IsActive);
                }
            }

            var activities = await query
                .OrderByDescending(a => a.ActivityDate)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.ActivityDate = activityDate?.ToString("yyyy-MM-dd");
            ViewBag.Status = status;

            return View(activities);
        }

        // GET: /Activity/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Activity/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ActivityTitle,ActivityDate,Location,Description")] Activity activity)
        {
            if (!ModelState.IsValid)
            {
                return View(activity);
            }

            activity.IsActive = true;
            activity.CreatedAt = DateTime.Now;
            activity.UpdatedAt = null;

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Activity added successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: /Activity/Edit/5
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var activity = await _context.Activities
                .FindAsync(id);

            if (activity == null)
            {
                return NotFound();
            }

            return View(activity);
        }

        // POST: /Activity/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("ActivityID,ActivityTitle,ActivityDate,Location,Description,IsActive")] Activity activity)
        {
            if (id != activity.ActivityID)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(activity);
            }

            var existingActivity = await _context.Activities
                .FindAsync(id);

            if (existingActivity == null)
            {
                return NotFound();
            }

            existingActivity.ActivityTitle = activity.ActivityTitle;
            existingActivity.ActivityDate = activity.ActivityDate;
            existingActivity.Location = activity.Location;
            existingActivity.Description = activity.Description;
            existingActivity.IsActive = activity.IsActive;
            existingActivity.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Activity updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: /Activity/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var activity = await _context.Activities
                .FirstOrDefaultAsync(a => a.ActivityID == id);

            if (activity == null)
            {
                return NotFound();
            }

            return View(activity);
        }

        // POST: /Activity/Deactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Deactivate(int id)
        {
            var activity = await _context.Activities
                .FindAsync(id);

            if (activity == null)
            {
                return NotFound();
            }

            activity.IsActive = false;
            activity.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Activity cancelled/deactivated.";

            return RedirectToAction(nameof(Index));
        }

        // POST: /Activity/Activate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Activate(int id)
        {
            var activity = await _context.Activities
                .FindAsync(id);

            if (activity == null)
            {
                return NotFound();
            }

            activity.IsActive = true;
            activity.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Activity activated successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}