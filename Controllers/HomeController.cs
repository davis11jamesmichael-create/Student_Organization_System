using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Data;
using StudentOrganizationSystem.Models;

namespace StudentOrganizationSystem.Controllers
{
    // Staff only (Admin or Officer): a Member account has no business on the
    // management screens. HomeController.Error carries [AllowAnonymous] so error
    // pages still render for everyone.
    [Authorize(Roles = AppRoles.AdminOrOfficer)]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public HomeController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            // P1.10: the old version issued six separate COUNT round trips for one
            // screen. This is two grouped queries (total + active per table) plus
            // one count for each of the two history tables. AsNoTracking() because
            // nothing here is ever written back.
            var memberCounts = await _context.Members.AsNoTracking()
                .GroupBy(m => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Active = g.Count(m => m.IsActive)
                })
                .FirstOrDefaultAsync();

            var activityCounts = await _context.Activities.AsNoTracking()
                .GroupBy(a => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Active = g.Count(a => a.IsActive)
                })
                .FirstOrDefaultAsync();

            var dashboard = new DashboardViewModel
            {
                TotalMembers = memberCounts?.Total ?? 0,
                ActiveMembers = memberCounts?.Active ?? 0,

                TotalActivities = activityCounts?.Total ?? 0,
                ActiveActivities = activityCounts?.Active ?? 0,

                TotalAttendance = await _context.Attendances
                    .AsNoTracking()
                    .CountAsync(),

                TotalParticipation = await _context.Participations
                    .AsNoTracking()
                    .CountAsync()
            };

            ViewBag.LatestAnnouncements = await _context.Announcements
                .AsNoTracking()
                .Where(a => a.IsActive &&
                    (a.Audience == AnnouncementAudience.All ||
                     a.Audience == AnnouncementAudience.Staff))
                .OrderByDescending(a => a.IsPinned)
                .ThenByDescending(a => a.CreatedAt)
                .Take(3)
                .ToListAsync();

            return View(dashboard);
        }

        // GET: /Home/Error
        //
        // The target of Program.cs UseExceptionHandler("/Home/Error") and of
        // UseStatusCodePagesWithReExecute(...). Before Phase 0 this action and the
        // view did not exist, so every unhandled exception was re-executed into a
        // 404 and the real cause was invisible (AUDIT.md P0.3).
        //
        // [AllowAnonymous] on purpose: a signed-out visitor must still see the page,
        // and the [Authorize] on the class would otherwise bounce them to Login.
        [AllowAnonymous]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Error(int? statusCode = null)
        {
            var exceptionFeature = HttpContext.Features
                .Get<IExceptionHandlerPathFeature>();

            var model = new ErrorViewModel
            {
                StatusCode = statusCode
                    ?? StatusCodes.Status500InternalServerError,

                // Activity.Current is the request id that also appears in the log.
                // Fully qualified: "Activity" is also the name of our own model.
                RequestId = System.Diagnostics.Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier,

                Path = exceptionFeature?.Path,

                // Only a development build shows the exception text on screen.
                Detail = _environment.IsDevelopment()
                    ? exceptionFeature?.Error.Message
                    : null
            };

            return View(model);
        }
    }
}
