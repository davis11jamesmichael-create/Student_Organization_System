using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Data;
using StudentOrganizationSystem.Models;

namespace StudentOrganizationSystem.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Attendance
        // Lists all activities so the user can pick one to take/view attendance
        public async Task<IActionResult> Index(string? search, string? status)
        {
            var query = _context.Activities.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(a => a.ActivityTitle.Contains(search));

            if (status == "Active")
                query = query.Where(a => a.IsActive);
            else if (status == "Inactive")
                query = query.Where(a => !a.IsActive);

            var activities = await query
                .OrderByDescending(a => a.ActivityDate)
                .ToListAsync();

            // For each activity, count how many attendance records exist
            var activityIds = activities.Select(a => a.ActivityID).ToList();

            var counts = await _context.Attendances
                .Where(a => activityIds.Contains(a.ActivityID))
                .GroupBy(a => a.ActivityID)
                .Select(g => new { ActivityID = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ActivityID, x => x.Count);

            ViewBag.AttendanceCounts = counts;
            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(activities);
        }

        // GET: /Attendance/Record/5
        // Shows the form to mark attendance for a specific activity
        [HttpGet]
        public async Task<IActionResult> Record(int? id)
        {
            if (id == null) return NotFound();

            var activity = await _context.Activities
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.ActivityID == id);

            if (activity == null) return NotFound();

            // Get all active members
            var members = await _context.Members
                .AsNoTracking()
                .Where(m => m.IsActive)
                .OrderBy(m => m.FullName)
                .ToListAsync();

            // Get existing attendance records for this activity
            var existing = await _context.Attendances
                .Where(a => a.ActivityID == id)
                .ToDictionaryAsync(a => a.MemberID, a => a.Status);

            ViewBag.Activity = activity;
            ViewBag.ExistingAttendance = existing;

            return View(members);
        }

        // POST: /Attendance/Record/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Record(
            int id,
            [FromForm] Dictionary<int, string> attendance)
        {
            var activity = await _context.Activities.FindAsync(id);
            if (activity == null) return NotFound();

            foreach (var entry in attendance)
            {
                int memberId = entry.Key;
                string status = entry.Value; // "Present" or "Absent"

                var existing = await _context.Attendances
                    .FirstOrDefaultAsync(a =>
                        a.ActivityID == id && a.MemberID == memberId);

                if (existing != null)
                {
                    // Update existing record
                    existing.Status = status;
                    existing.RecordedAt = DateTime.Now;
                }
                else
                {
                    // Create new record
                    _context.Attendances.Add(new Attendance
                    {
                        ActivityID = id,
                        MemberID = memberId,
                        Status = status,
                        RecordedAt = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Attendance saved for \"{activity.ActivityTitle}\".";

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /Attendance/Details/5
        // Shows the saved attendance list for a specific activity
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var activity = await _context.Activities
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.ActivityID == id);

            if (activity == null) return NotFound();

            var records = await _context.Attendances
                .AsNoTracking()
                .Include(a => a.Member)
                .Where(a => a.ActivityID == id)
                .OrderBy(a => a.Member!.FullName)
                .ToListAsync();

            ViewBag.Activity = activity;

            return View(records);
        }
    }
}
