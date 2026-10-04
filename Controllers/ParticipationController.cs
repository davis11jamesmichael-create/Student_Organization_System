using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Data;
using StudentOrganizationSystem.Models;

namespace StudentOrganizationSystem.Controllers
{
    // 3H - Participation Monitoring. Staff only (Admin or Officer), the same rule
    // as AttendanceController: recording participation is the Officer's day-to-day
    // work, so it is not Admin-gated.
    [Authorize(Roles = AppRoles.AdminOrOfficer)]
    public class ParticipationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ParticipationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Participation
        // Lists activities so the user can pick one to record or view participation.
        public async Task<IActionResult> Index(string? search, string? status, int page = 1)
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

            // P3: one page at a time, with the filters carried into the pager.
            var paged = await Paging.ToPagedAsync(
                query.OrderByDescending(a => a.ActivityDate),
                page);

            var activities = paged.Items;

            var activityIds = activities.Select(a => a.ActivityID).ToList();

            var counts = await _context.Participations
                .Where(p => activityIds.Contains(p.ActivityID))
                .GroupBy(p => p.ActivityID)
                .Select(g => new { ActivityID = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ActivityID, x => x.Count);

            ViewBag.ParticipationCounts = counts;
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Pager = paged.ToPager(
                "Participation",
                "Index",
                new Dictionary<string, string>
                {
                    ["search"] = search ?? string.Empty,
                    ["status"] = status ?? string.Empty
                });

            return View(activities);
        }

        // GET: /Participation/Record/5
        // One text box per active member; a blank box means "not recorded".
        [HttpGet]
        public async Task<IActionResult> Record(int? id)
        {
            if (id == null) return NotFound();

            var activity = await _context.Activities
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.ActivityID == id);

            if (activity == null) return NotFound();

            // P1.3 rule, applied to participation: a cancelled activity takes none.
            if (!activity.IsActive)
            {
                TempData["Error"] =
                    $"\"{activity.ActivityTitle}\" is cancelled, so participation "
                    + "can no longer be recorded for it.";

                return RedirectToAction(nameof(Details), new { id });
            }

            var members = await _context.Members
                .AsNoTracking()
                .Where(m => m.IsActive)
                .OrderBy(m => m.FullName)
                .ToListAsync();

            var existing = await _context.Participations
                .AsNoTracking()
                .Where(p => p.ActivityID == id)
                .ToDictionaryAsync(p => p.MemberID, p => p.ParticipationType);

            ViewBag.Activity = activity;
            ViewBag.ExistingParticipation = existing;

            return View(members);
        }

        // POST: /Participation/Record/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Record(
            int id,
            [FromForm] Dictionary<int, string> participation)
        {
            var activity = await _context.Activities.FindAsync(id);
            if (activity == null) return NotFound();

            if (!activity.IsActive)
            {
                TempData["Error"] =
                    $"\"{activity.ActivityTitle}\" is cancelled, so participation "
                    + "can no longer be recorded for it.";

                return RedirectToAction(nameof(Details), new { id });
            }

            // P1.6: trim every value, and reject anything too long for the column.
            var posted = participation.ToDictionary(
                entry => entry.Key,
                entry => ParticipationTypes.Normalize(entry.Value));

            if (posted.Values.Any(v => v.Length > ParticipationTypes.MaxLength))
            {
                TempData["Error"] =
                    $"A participation type must be {ParticipationTypes.MaxLength} "
                    + "characters or fewer.";

                return RedirectToAction(nameof(Record), new { id });
            }

            // Only real, active members may be written (the P1.2 rule).
            var postedIds = posted.Keys.ToList();

            var validMemberIds = await _context.Members
                .AsNoTracking()
                .Where(m => m.IsActive && postedIds.Contains(m.MemberID))
                .Select(m => m.MemberID)
                .ToListAsync();

            // One read for the whole activity (the P1.7 lesson), then upsert per
            // the (ActivityID, MemberID) unique index.
            var existing = await _context.Participations
                .Where(p => p.ActivityID == id)
                .ToDictionaryAsync(p => p.MemberID);

            var recorded = 0;
            var cleared = 0;

            foreach (var memberId in validMemberIds)
            {
                var type = posted[memberId];

                if (string.IsNullOrEmpty(type))
                {
                    // A blank box means "not recorded", so drop any old row.
                    if (existing.TryGetValue(memberId, out var blankRecord))
                    {
                        _context.Participations.Remove(blankRecord);
                        cleared++;
                    }

                    continue;
                }

                if (existing.TryGetValue(memberId, out var record))
                {
                    // P1.8 rule: RecordedAt keeps the first recording time.
                    record.ParticipationType = type;
                }
                else
                {
                    _context.Participations.Add(new Participation
                    {
                        ActivityID = id,
                        MemberID = memberId,
                        ParticipationType = type,
                        RecordedAt = DateTime.Now
                    });
                }

                recorded++;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // The unique index on (ActivityID, MemberID) is the real guard.
                TempData["Error"] =
                    "Participation was saved by another request a moment ago. "
                    + "Please reload the page and try again.";

                return RedirectToAction(nameof(Record), new { id });
            }

            TempData["Success"] =
                $"Participation saved for \"{activity.ActivityTitle}\" "
                + $"({recorded} recorded, {cleared} cleared).";

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /Participation/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var activity = await _context.Activities
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.ActivityID == id);

            if (activity == null) return NotFound();

            var records = await _context.Participations
                .AsNoTracking()
                .Include(p => p.Member)
                .Where(p => p.ActivityID == id)
                .OrderBy(p => p.Member!.FullName)
                .ToListAsync();

            ViewBag.Activity = activity;

            return View(records);
        }
    }
}
