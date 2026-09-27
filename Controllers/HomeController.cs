using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Data;
using StudentOrganizationSystem.Models;

namespace StudentOrganizationSystem.Controllers
{
    [Authorize]
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
            var dashboard = new DashboardViewModel
            {
                TotalMembers = await _context.Members.CountAsync(),
                ActiveMembers = await _context.Members
                    .CountAsync(m => m.IsActive),

                TotalActivities = await _context.Activities.CountAsync(),

                ActiveActivities = await _context.Activities
                    .CountAsync(a => a.IsActive),

                TotalAttendance = await _context.Attendances.CountAsync(),

                TotalParticipation =
                    await _context.Participations.CountAsync()
            };

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
