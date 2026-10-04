using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Data;
using StudentOrganizationSystem.Models;

namespace StudentOrganizationSystem.Controllers
{
    // Staff only (Admin or Officer). A "Member" login is confined to PortalController.
    [Authorize(Roles = AppRoles.AdminOrOfficer)]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Attendance
        // Lists all activities so the user can pick one to take/view attendance
        public async Task<IActionResult> Index(string? search, string? status, int page = 1)
        {
            var query = _context.Activities.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                // P1.6: trim the term so a stray space cannot hide a match.
                search = search.Trim();

                query = query.Where(a => a.ActivityTitle.Contains(search));
            }

            if (status == "Active")
                query = query.Where(a => a.IsActive);
            else if (status == "Inactive")
                query = query.Where(a => !a.IsActive);

            // P3: one page at a time, with the filters carried into the pager.
            var paged = await Paging.ToPagedAsync(
                query.OrderByDescending(a => a.ActivityDate),
                page);

            var activities = paged.Items;

            // For each activity on this page, count how many attendance records exist
            var activityIds = activities.Select(a => a.ActivityID).ToList();

            var counts = await _context.Attendances
                .Where(a => activityIds.Contains(a.ActivityID))
                .GroupBy(a => a.ActivityID)
                .Select(g => new { ActivityID = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ActivityID, x => x.Count);

            ViewBag.AttendanceCounts = counts;
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Pager = paged.ToPager(
                "Attendance",
                "Index",
                new Dictionary<string, string>
                {
                    ["search"] = search ?? string.Empty,
                    ["status"] = status ?? string.Empty
                });

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

            // P1.3: a cancelled (inactive) activity takes no new attendance, but
            // its saved history must stay readable, so send the user to Details.
            if (!activity.IsActive)
            {
                TempData["Error"] =
                    $"\"{activity.ActivityTitle}\" is cancelled, so attendance "
                    + "can no longer be recorded for it.";

                return RedirectToAction(nameof(Details), new { id });
            }

            // Get all active members
            var members = await _context.Members
                .AsNoTracking()
                .Where(m => m.IsActive)
                .OrderBy(m => m.FullName)
                .ToListAsync();

            // Get existing attendance records for this activity, keeping only
            // known statuses. The view starts a member's radios unmarked when
            // there is no valid saved value, so a status is never invented (P1.1).
            var saved = await _context.Attendances
                .AsNoTracking()
                .Where(a => a.ActivityID == id)
                .ToDictionaryAsync(a => a.MemberID, a => a.Status);

            var existing = saved
                .Where(pair => AttendanceStatus.IsValid(pair.Value))
                .ToDictionary(pair => pair.Key, pair => pair.Value);

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

            // P1.3: refuse to record against a cancelled activity.
            if (!activity.IsActive)
            {
                TempData["Error"] =
                    $"\"{activity.ActivityTitle}\" is cancelled, so attendance "
                    + "can no longer be recorded for it.";

                return RedirectToAction(nameof(Details), new { id });
            }

            // P1.2: whitelist the status server-side. A typo, a stale form or a
            // hand-crafted POST can no longer store free text in the Status column.
            var posted = attendance
                .Where(entry => AttendanceStatus.IsValid(entry.Value))
                .ToDictionary(entry => entry.Key, entry => entry.Value);

            if (posted.Count == 0)
            {
                TempData["Error"] =
                    "No valid attendance was submitted. Please mark the members "
                    + "and try again.";

                return RedirectToAction(nameof(Record), new { id });
            }

            // P1.2: only real, active members may be marked. Anything else is
            // silently dropped instead of writing an orphaned row.
            var postedIds = posted.Keys.ToList();

            var validMemberIds = await _context.Members
                .AsNoTracking()
                .Where(m => m.IsActive && postedIds.Contains(m.MemberID))
                .Select(m => m.MemberID)
                .ToListAsync();

            // P1.7: load every existing row for this activity in one query and
            // update or add from the dictionary. One read plus one write instead
            // of one read per member (the old N+1 loop).
            var existing = await _context.Attendances
                .Where(a => a.ActivityID == id)
                .ToDictionaryAsync(a => a.MemberID);

            var now = DateTime.Now;

            foreach (var memberId in validMemberIds)
            {
                var status = posted[memberId];

                if (existing.TryGetValue(memberId, out var record))
                {
                    if (record.Status != status)
                    {
                        record.Status = status;

                        // P1.8 / D4: RecordedAt keeps the first recording time;
                        // the change is stamped in UpdatedAt instead.
                        record.UpdatedAt = now;
                    }
                }
                else
                {
                    // Create new record
                    _context.Attendances.Add(new Attendance
                    {
                        ActivityID = id,
                        MemberID = memberId,
                        Status = status,
                        RecordedAt = now
                    });
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // The unique index on (ActivityID, MemberID) is the real guard:
                // two overlapping saves can both miss the batched read above.
                TempData["Error"] =
                    "Attendance was saved by another request a moment ago. "
                    + "Please reload the page and try again.";

                return RedirectToAction(nameof(Record), new { id });
            }

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
