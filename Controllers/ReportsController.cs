using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Data;
using StudentOrganizationSystem.Models;

namespace StudentOrganizationSystem.Controllers
{
    // 3I - Reports. Staff only (Admin or Officer); every page is read-only.
    [Authorize(Roles = AppRoles.AdminOrOfficer)]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Reports
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Reports/Attendance - per-activity attendance summary
        public async Task<IActionResult> Attendance(string? search, string? status)
        {
            var query = _context.Activities.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(a => a.ActivityTitle.Contains(search));
            }

            if (status == "Active")
            {
                query = query.Where(a => a.IsActive);
            }
            else if (status == "Inactive")
            {
                query = query.Where(a => !a.IsActive);
            }

            // P1.10: one projection; the counts are done in SQL, not per row in C#.
            var rows = await query
                .OrderByDescending(a => a.ActivityDate)
                .Select(a => new ActivityAttendanceReportRow
                {
                    ActivityID = a.ActivityID,
                    ActivityTitle = a.ActivityTitle,
                    ActivityDate = a.ActivityDate,
                    IsActive = a.IsActive,

                    Present = a.Attendances
                        .Count(x => x.Status == AttendanceStatus.Present),

                    Absent = a.Attendances
                        .Count(x => x.Status == AttendanceStatus.Absent)
                })
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.TotalPresent = rows.Sum(r => r.Present);
            ViewBag.TotalAbsent = rows.Sum(r => r.Absent);

            return View(rows);
        }

        // GET: /Reports/Members - per-member attendance and participation summary
        public async Task<IActionResult> Members(string? search)
        {
            var query = _context.Members.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(m =>
                    m.StudentID.Contains(search) ||
                    m.FullName.Contains(search));
            }

            var rows = await query
                .OrderBy(m => m.FullName)
                .Select(m => new MemberReportRow
                {
                    MemberID = m.MemberID,
                    StudentID = m.StudentID,
                    FullName = m.FullName,
                    IsActive = m.IsActive,

                    Standing = m.Memberships
                        .Select(x => x.Status)
                        .FirstOrDefault() ?? string.Empty,

                    Present = m.Attendances
                        .Count(a => a.Status == AttendanceStatus.Present),

                    Absent = m.Attendances
                        .Count(a => a.Status == AttendanceStatus.Absent),

                    ParticipationCount = m.Participations.Count()
                })
                .ToListAsync();

            ViewBag.Search = search;

            return View(rows);
        }
    }
}
