using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Data;

namespace StudentOrganizationSystem.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
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
    }
}