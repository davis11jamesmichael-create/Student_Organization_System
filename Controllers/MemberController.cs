using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Data;
using StudentOrganizationSystem.Models;

namespace StudentOrganizationSystem.Controllers
{
    // Role policy (AUDIT.md P0.6, decision D3; extended to "Admin-only Create"):
    //   any signed-in user -> Index and Details (read-only);
    //   Admin only         -> registering a new member, and changing or
    //                         (de)activating an existing one, because those
    //                         actions create or change a member's record.
    // Staff only (Admin or Officer). A "Member" login is confined to PortalController.
    [Authorize(Roles = AppRoles.AdminOrOfficer)]
    public class MemberController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MemberController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Member
        public async Task<IActionResult> Index(
            string? search,
            string? status,
            int page = 1)
        {
            var members = _context.Members
                .AsNoTracking()
                .AsQueryable();

            // Search by Student ID or name
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                members = members.Where(m =>
                    m.StudentID.Contains(search) ||
                    m.FullName.Contains(search));
            }

            // Filter by active/inactive
            if (status == "Active")
            {
                members = members.Where(m => m.IsActive);
            }
            else if (status == "Inactive")
            {
                members = members.Where(m => !m.IsActive);
            }

            // P3: one page at a time, with the filters carried into the pager.
            var paged = await Paging.ToPagedAsync(
                members.OrderBy(m => m.FullName),
                page);

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Pager = paged.ToPager(
                "Member",
                "Index",
                new Dictionary<string, string>
                {
                    ["search"] = search ?? string.Empty,
                    ["status"] = status ?? string.Empty
                });

            return View(paged.Items);
        }


        // GET: /Member/Create
        // Admin only: registering a member is an administrative action, so it
        // carries the same role guard as Edit / Activate / Deactivate. The
        // matching "+ Add Member" links are hidden from Officers in the views.
        [HttpGet]
        [Authorize(Roles = AppRoles.Admin)]
        public IActionResult Create()
        {
            return View();
        }


        // POST: /Member/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Create([Bind("StudentID,FullName,Course,YearLevel")] Member member)
        {
            // P1.6: trim before validating and saving. MySQL 8 uses a NO PAD
            // collation, so "2024-00001 " and "2024-00001" would otherwise be
            // stored as two different values and both pass the unique index.
            member.StudentID = (member.StudentID ?? string.Empty).Trim();
            member.FullName = (member.FullName ?? string.Empty).Trim();
            member.Course = (member.Course ?? string.Empty).Trim();
            member.YearLevel = (member.YearLevel ?? string.Empty).Trim();

            // The binder validated the untrimmed text. Validate again now that the
            // values are clean, so a harmless trailing space is accepted instead of
            // failing the format rule.
            ModelState.Clear();
            TryValidateModel(member);

            if (!ModelState.IsValid)
            {
                return View(member);
            }

            // Prevent duplicate Student ID
            var existingMember = await _context.Members
                .FirstOrDefaultAsync(
                    m => m.StudentID == member.StudentID);

            if (existingMember != null)
            {
                ModelState.AddModelError(
                    "StudentID",
                    "This Student ID is already registered.");

                return View(member);
            }

            member.IsActive = true;
            member.CreatedAt = DateTime.Now;

            // Decision D2: a new member starts with a membership row whose
            // standing matches the active flag.
            MembershipPolicy.Ensure(member);

            _context.Members.Add(member);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // P1.4: the unique index on Members.StudentID is the real guard.
                // Two overlapping submissions can both pass the pre-check above,
                // so the race becomes a field error instead of an unhandled 500.
                ModelState.AddModelError(
                    "StudentID",
                    "This Student ID is already registered.");

                return View(member);
            }

            TempData["Success"] =
                "Member successfully registered.";

            return RedirectToAction(nameof(Index));
        }


        // GET: /Member/Edit/5
        [HttpGet]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var member = await _context.Members
                .FindAsync(id);

            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }


        // POST: /Member/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("MemberID,StudentID,FullName,Course,YearLevel,IsActive")] Member member)
        {
            if (id != member.MemberID)
            {
                return NotFound();
            }

            // P1.6: trim and re-validate, exactly as in Create. Without this the
            // duplicate check below would treat "2024-00001 " as a new value.
            member.StudentID = (member.StudentID ?? string.Empty).Trim();
            member.FullName = (member.FullName ?? string.Empty).Trim();
            member.Course = (member.Course ?? string.Empty).Trim();
            member.YearLevel = (member.YearLevel ?? string.Empty).Trim();

            ModelState.Clear();
            TryValidateModel(member);

            if (!ModelState.IsValid)
            {
                return View(member);
            }

            var duplicateStudentID =
                await _context.Members.AnyAsync(
                    m => m.StudentID == member.StudentID &&
                         m.MemberID != member.MemberID);

            if (duplicateStudentID)
            {
                ModelState.AddModelError(
                    "StudentID",
                    "This Student ID is already used by another member.");

                return View(member);
            }

            var existingMember = await _context.Members
                .Include(m => m.Memberships)
                .FirstOrDefaultAsync(m => m.MemberID == id);

            if (existingMember == null)
            {
                return NotFound();
            }

            existingMember.StudentID = member.StudentID;
            existingMember.FullName = member.FullName;
            existingMember.Course = member.Course;
            existingMember.YearLevel = member.YearLevel;
            existingMember.IsActive = member.IsActive;
            existingMember.UpdatedAt = DateTime.Now;

            // Decision D2: this form can flip the active flag, so keep the
            // membership standing in step (an inactive member is never "Active").
            MembershipPolicy.SyncStanding(existingMember);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // P1.4: the unique index on Members.StudentID is the real guard.
                ModelState.AddModelError(
                    "StudentID",
                    "This Student ID is already used by another member.");

                return View(member);
            }

            // P1.5: the old DbUpdateConcurrencyException catch was deleted on
            // purpose. No model carries a concurrency token, so that block could
            // never run - it only implied a guarantee that did not exist.
            TempData["Success"] =
                "Member information successfully updated.";

            return RedirectToAction(nameof(Index));
        }


        // GET: /Member/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var member = await _context.Members
                .AsNoTracking()
                .Include(m => m.Memberships)
                .FirstOrDefaultAsync(
                    m => m.MemberID == id);

            if (member == null)
            {
                return NotFound();
            }

            // Module 3F "member history": the membership standing, a couple of
            // counters, and the newest attendance and participation rows.
            // P1.10: the attendance counters are one grouped query, not three.
            var attendanceCounts = await _context.Attendances
                .AsNoTracking()
                .Where(a => a.MemberID == id)
                .GroupBy(a => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Present = g.Count(x => x.Status == AttendanceStatus.Present),
                    Absent = g.Count(x => x.Status == AttendanceStatus.Absent)
                })
                .FirstOrDefaultAsync();

            ViewBag.PresentCount = attendanceCounts?.Present ?? 0;
            ViewBag.AbsentCount = attendanceCounts?.Absent ?? 0;
            ViewBag.AttendanceTotal = attendanceCounts?.Total ?? 0;

            ViewBag.ParticipationCount = await _context.Participations
                .AsNoTracking()
                .CountAsync(p => p.MemberID == id);

            ViewBag.RecentAttendance = await _context.Attendances
                .AsNoTracking()
                .Include(a => a.Activity)
                .Where(a => a.MemberID == id)
                .OrderByDescending(a => a.RecordedAt)
                .Take(10)
                .ToListAsync();

            ViewBag.RecentParticipation = await _context.Participations
                .AsNoTracking()
                .Include(p => p.Activity)
                .Where(p => p.MemberID == id)
                .OrderByDescending(p => p.RecordedAt)
                .Take(10)
                .ToListAsync();

            return View(member);
        }


        // POST: /Member/Deactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Deactivate(int id)
        {
            var member = await _context.Members
                .Include(m => m.Memberships)
                .FirstOrDefaultAsync(m => m.MemberID == id);

            if (member == null)
            {
                return NotFound();
            }

            member.IsActive = false;
            member.UpdatedAt = DateTime.Now;

            // Decision D2: an inactive member's standing becomes "Inactive".
            MembershipPolicy.SyncStanding(member);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Member has been deactivated.";

            return RedirectToAction(nameof(Index));
        }


        // POST: /Member/Activate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Activate(int id)
        {
            var member = await _context.Members
                .Include(m => m.Memberships)
                .FirstOrDefaultAsync(m => m.MemberID == id);

            if (member == null)
            {
                return NotFound();
            }

            member.IsActive = true;
            member.UpdatedAt = DateTime.Now;

            // Decision D2: reactivating returns the standing to "Active"
            // (a "Suspended" standing is kept).
            MembershipPolicy.SyncStanding(member);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Member has been activated.";

            return RedirectToAction(nameof(Index));
        }
    }
}