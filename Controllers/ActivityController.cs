using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Data;
using StudentOrganizationSystem.Models;

namespace StudentOrganizationSystem.Controllers
{
    // Role policy (AUDIT.md P0.6, decision D3; extended to "Admin-only Create"):
    //   any signed-in user -> Index, Details, and recording attendance (that is
    //                         the Officer's day-to-day work);
    //   Admin only         -> adding a new activity, and changing or
    //                         (de)activating an existing one.
    // Staff only (Admin or Officer). A "Member" login is confined to PortalController.
    [Authorize(Roles = AppRoles.AdminOrOfficer)]
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
            string? status,
            int page = 1)
        {
            // P1.10: this list is read-only, so the entities are not tracked.
            var query = _context.Activities.AsNoTracking().AsQueryable();

            // Search by activity title
            if (!string.IsNullOrWhiteSpace(search))
            {
                // P1.6: trim the term so a stray space cannot hide a match.
                search = search.Trim();

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

            // P3: one page at a time, with the filters carried into the pager.
            var paged = await Paging.ToPagedAsync(
                query.OrderByDescending(a => a.ActivityDate),
                page);

            ViewBag.Search = search;
            ViewBag.ActivityDate = activityDate?.ToString("yyyy-MM-dd");
            ViewBag.Status = status;
            ViewBag.Pager = paged.ToPager(
                "Activity",
                "Index",
                new Dictionary<string, string>
                {
                    ["search"] = search ?? string.Empty,
                    ["activityDate"] = activityDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                    ["status"] = status ?? string.Empty
                });

            return View(paged.Items);
        }

        // GET: /Activity/Create
        // Admin only: adding an activity is an administrative action, so it
        // carries the same role guard as Edit / Activate / Deactivate. The
        // matching "+ Add Activity" links are hidden from Officers in the views.
        [Authorize(Roles = AppRoles.Admin)]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Activity/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Create([Bind("ActivityTitle,ActivityDate,Location,Description")] Activity activity)
        {
            // P1.6: trim before validating and saving, so "  Quiz Bee  " and
            // "Quiz Bee" never become two different activities.
            activity.ActivityTitle = (activity.ActivityTitle ?? string.Empty).Trim();
            activity.Location = (activity.Location ?? string.Empty).Trim();
            activity.Description = activity.Description?.Trim();

            // Re-validate the cleaned values (the binder saw the untrimmed text).
            ModelState.Clear();
            TryValidateModel(activity);

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

            // P1.10: read-only load for the form.
            var activity = await _context.Activities
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.ActivityID == id);

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

            // P1.6: trim and re-validate, exactly as in Create.
            activity.ActivityTitle = (activity.ActivityTitle ?? string.Empty).Trim();
            activity.Location = (activity.Location ?? string.Empty).Trim();
            activity.Description = activity.Description?.Trim();

            ModelState.Clear();
            TryValidateModel(activity);

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
                .AsNoTracking()
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