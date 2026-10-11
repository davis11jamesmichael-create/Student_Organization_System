using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Data;
using StudentOrganizationSystem.Models;

namespace StudentOrganizationSystem.Controllers
{
    // The Member portal: read-only access to the signed-in member's OWN records.
    // A Member can never reach a management controller, because every one of them
    // is [Authorize(Roles = AppRoles.AdminOrOfficer)].
    [Authorize(Roles = AppRoles.Member)]
    public class PortalController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PortalController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Portal
        public async Task<IActionResult> Index()
        {
            var member = await GetCurrentMemberAsync();

            if (member == null)
            {
                return View("NotLinked");
            }

            ViewBag.PresentCount = await _context.Attendances
                .AsNoTracking()
                .CountAsync(a => a.MemberID == member.MemberID
                              && a.Status == "Present");

            ViewBag.AbsentCount = await _context.Attendances
                .AsNoTracking()
                .CountAsync(a => a.MemberID == member.MemberID
                              && a.Status == "Absent");

            ViewBag.TotalCount = await _context.Attendances
                .AsNoTracking()
                .CountAsync(a => a.MemberID == member.MemberID);

            ViewBag.ParticipationCount = await _context.Participations
                .AsNoTracking()
                .CountAsync(p => p.MemberID == member.MemberID);

            ViewBag.LatestAnnouncements = await _context.Announcements
                .AsNoTracking()
                .Where(a => a.IsActive &&
                    (a.Audience == AnnouncementAudience.All ||
                     a.Audience == AnnouncementAudience.Members))
                .OrderByDescending(a => a.IsPinned)
                .ThenByDescending(a => a.CreatedAt)
                .Take(3)
                .ToListAsync();

            return View(member);
        }

        // GET: /Portal/History
        // One combined table for attendance AND participation: the two records are
        // keyed by the same activity, so a row shows the participation type and the
        // attendance status together (module 4a).
        public async Task<IActionResult> History()
        {
            var member = await GetCurrentMemberAsync();

            if (member == null)
            {
                return View("NotLinked");
            }

            var attendances = await _context.Attendances
                .AsNoTracking()
                .Include(a => a.Activity)
                .Where(a => a.MemberID == member.MemberID)
                .ToListAsync();

            var participations = await _context.Participations
                .AsNoTracking()
                .Include(p => p.Activity)
                .Where(p => p.MemberID == member.MemberID)
                .ToListAsync();

            ViewBag.Member = member;

            return View(MemberRecords.Combine(attendances, participations));
        }

        // GET: /Portal/Activities/5
        // B3: Member-side entry point for the discussion thread on one activity.
        // The activity id comes from the URL, but is validated against the member's
        // own attendance/participation records so a member cannot browse arbitrary
        // activities by editing the address.
        public async Task<IActionResult> Activities(int? id)
        {
            if (id == null) return NotFound();

            var member = await GetCurrentMemberAsync();
            if (member == null) return View("NotLinked");

            // Scope: the member must have an attendance or participation record for
            // this activity, OR the activity is upcoming (so they can discuss it
            // before it happens).
            var hasRecord = await _context.Attendances
                .AsNoTracking()
                .AnyAsync(a => a.ActivityID == id && a.MemberID == member.MemberID);

            if (!hasRecord)
            {
                hasRecord = await _context.Participations
                    .AsNoTracking()
                    .AnyAsync(p => p.ActivityID == id && p.MemberID == member.MemberID);
            }

            if (!hasRecord)
            {
                // Allow upcoming active activities even without a record yet.
                hasRecord = await _context.Activities
                    .AsNoTracking()
                    .AnyAsync(a => a.ActivityID == id && a.IsActive &&
                                   a.ActivityDate >= DateTime.Today);
            }

            if (!hasRecord) return Forbid();

            var activity = await _context.Activities
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.ActivityID == id && a.IsActive);

            if (activity == null) return NotFound();

            var comments = await _context.ActivityComments
                .AsNoTracking()
                .Include(c => c.User)
                .Where(c => c.ActivityID == id && c.IsActive)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.Comments = comments;
            ViewBag.CommentActivityId = activity.ActivityID;

            return View(activity);
        }

        /// <summary>
        /// Resolves the Members row from the MemberID claim written into the cookie
        /// at sign-in. The id is never taken from the URL, so a member cannot read
        /// another member's records by editing an address.
        /// </summary>
        private async Task<Member?> GetCurrentMemberAsync()
        {
            var value = User.FindFirst(AppClaims.MemberId)?.Value;

            if (!int.TryParse(value, out var memberId))
            {
                return null;
            }

            return await _context.Members
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MemberID == memberId);
        }
    }
}