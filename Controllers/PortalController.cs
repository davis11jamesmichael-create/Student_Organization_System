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