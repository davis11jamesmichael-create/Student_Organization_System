using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Data;
using StudentOrganizationSystem.Models;

namespace StudentOrganizationSystem.Controllers
{
    [Authorize]
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
            string? status)
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

            var result = await members
                .OrderBy(m => m.FullName)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(result);
        }


        // GET: /Member/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }


        // POST: /Member/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Member member)
        {
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

            _context.Members.Add(member);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Member successfully registered.";

            return RedirectToAction(nameof(Index));
        }


        // GET: /Member/Edit/5
        [HttpGet]
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
        public async Task<IActionResult> Edit(
            int id,
            Member member)
        {
            if (id != member.MemberID)
            {
                return NotFound();
            }

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

            try
            {
                var existingMember =
                    await _context.Members.FindAsync(id);

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

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Member information successfully updated.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await MemberExists(member.MemberID))
                {
                    return NotFound();
                }

                throw;
            }

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
                .FirstOrDefaultAsync(
                    m => m.MemberID == id);

            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }


        // POST: /Member/Deactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var member = await _context.Members
                .FindAsync(id);

            if (member == null)
            {
                return NotFound();
            }

            member.IsActive = false;
            member.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Member has been deactivated.";

            return RedirectToAction(nameof(Index));
        }


        // POST: /Member/Activate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var member = await _context.Members
                .FindAsync(id);

            if (member == null)
            {
                return NotFound();
            }

            member.IsActive = true;
            member.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Member has been activated.";

            return RedirectToAction(nameof(Index));
        }


        private async Task<bool> MemberExists(int id)
        {
            return await _context.Members
                .AnyAsync(m => m.MemberID == id);
        }
    }
}