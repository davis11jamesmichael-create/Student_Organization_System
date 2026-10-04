using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Data;
using StudentOrganizationSystem.Models;

namespace StudentOrganizationSystem.Controllers
{
    // 3G - Membership Management (AUDIT.md decision D2).
    //   any signed-in staff -> read the list and a member's membership;
    //   Admin only          -> change a standing. That is a status change, so it
    //                          carries the same guard Phase 0 put on
    //                          Activate/Deactivate (AUDIT.md P0.6 / D3).
    [Authorize(Roles = AppRoles.AdminOrOfficer)]
    public class MembershipController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MembershipController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Membership
        public async Task<IActionResult> Index(string? search, string? status, int page = 1)
        {
            var members = _context.Members
                .AsNoTracking()
                .AsQueryable();

            // Search by Student ID or name (P1.6: trim the term).
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                members = members.Where(m =>
                    m.StudentID.Contains(search) ||
                    m.FullName.Contains(search));
            }

            // Filter by standing. "None" means the member has no membership row.
            if (status == "None")
            {
                members = members.Where(m => !m.Memberships.Any());
            }
            else if (MembershipStatus.IsValid(status))
            {
                members = members.Where(m =>
                    m.Memberships.Any(x => x.Status == status));
            }

            // One query: the member plus its single membership row (D2).
            var projected = members
                .OrderBy(m => m.FullName)
                .Select(m => new MembershipViewModel
                {
                    MemberID = m.MemberID,
                    StudentID = m.StudentID,
                    FullName = m.FullName,
                    Course = m.Course,
                    YearLevel = m.YearLevel,
                    IsActive = m.IsActive,

                    MembershipID = m.Memberships
                        .Select(x => (int?)x.MembershipID)
                        .FirstOrDefault(),

                    DateJoined = m.Memberships
                        .Select(x => (DateTime?)x.DateJoined)
                        .FirstOrDefault(),

                    Status = m.Memberships
                        .Select(x => x.Status)
                        .FirstOrDefault() ?? string.Empty,

                    UpdatedAt = m.Memberships
                        .Select(x => (DateTime?)x.UpdatedAt)
                        .FirstOrDefault()
                });

            // P3: one page at a time, with the filters carried into the pager.
            var paged = await Paging.ToPagedAsync(projected, page);

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.UnsetCount = await _context.Members
                .AsNoTracking()
                .CountAsync(m => !m.Memberships.Any());
            ViewBag.Pager = paged.ToPager(
                "Membership",
                "Index",
                new Dictionary<string, string>
                {
                    ["search"] = search ?? string.Empty,
                    ["status"] = status ?? string.Empty
                });

            return View(paged.Items);
        }

        // GET: /Membership/Edit/5   (5 is the MemberID - a membership is 1:1)
        [HttpGet]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var member = await _context.Members
                .AsNoTracking()
                .Include(m => m.Memberships)
                .FirstOrDefaultAsync(m => m.MemberID == id);

            if (member == null)
            {
                return NotFound();
            }

            return View(ToViewModel(member));
        }

        // POST: /Membership/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("MemberID,Status")] MembershipViewModel model)
        {
            if (id != model.MemberID)
            {
                return NotFound();
            }

            var member = await _context.Members
                .Include(m => m.Memberships)
                .FirstOrDefaultAsync(m => m.MemberID == id);

            if (member == null)
            {
                return NotFound();
            }

            // P1.2-style whitelist: only the two standings an Admin may set by hand.
            // "Inactive" is owned by the member's own Activate/Deactivate control, so
            // the two fields can never disagree (D2).
            if (!MembershipStatus.Selectable.Contains(model.Status))
            {
                ModelState.AddModelError(
                    "Status",
                    "Choose Active or Suspended. Use the member's "
                        + "Activate / Deactivate control to change whether they "
                        + "are active.");
            }

            if (!member.IsActive)
            {
                ModelState.AddModelError(
                    "Status",
                    "This member is deactivated, so their standing is Inactive. "
                        + "Activate the member first.");
            }

            if (!ModelState.IsValid)
            {
                return View(ToViewModel(member));
            }

            // Upsert: a legacy member with no row gets one on first save.
            var membership = MembershipPolicy.Ensure(member);
            membership.Status = model.Status;
            membership.UpdatedAt = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // The unique index on Memberships.MemberID is the real guard.
                ModelState.AddModelError(
                    "",
                    "This member already has a membership record. Please reload "
                        + "the page and try again.");

                return View(ToViewModel(member));
            }

            TempData["Success"] =
                $"Membership for \"{member.FullName}\" is now {model.Status}.";

            return RedirectToAction(nameof(Index));
        }

        private static MembershipViewModel ToViewModel(Member member)
        {
            var membership = member.Memberships.FirstOrDefault();

            return new MembershipViewModel
            {
                MemberID = member.MemberID,
                StudentID = member.StudentID,
                FullName = member.FullName,
                Course = member.Course,
                YearLevel = member.YearLevel,
                IsActive = member.IsActive,
                MembershipID = membership?.MembershipID,
                DateJoined = membership?.DateJoined ?? member.CreatedAt,
                Status = membership?.Status
                    ?? MembershipPolicy.StandingFor(member.IsActive),
                UpdatedAt = membership?.UpdatedAt
            };
        }
    }
}
