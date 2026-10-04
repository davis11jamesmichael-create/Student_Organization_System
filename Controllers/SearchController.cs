using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Data;
using StudentOrganizationSystem.Models;

namespace StudentOrganizationSystem.Controllers
{
    // 3J - Cross-module search. Staff only (Admin or Officer); read-only.
    [Authorize(Roles = AppRoles.AdminOrOfficer)]
    public class SearchController : Controller
    {
        /// <summary>A broad term must not pull the whole database into memory.</summary>
        private const int MaxPerType = 50;

        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Search?q=...
        public async Task<IActionResult> Index(string? q)
        {
            // P1.6: trim the term once, then use it for every query.
            q = q?.Trim();

            var model = new SearchResultsViewModel
            {
                Query = q ?? string.Empty
            };

            if (!string.IsNullOrWhiteSpace(q))
            {
                model.Members = await _context.Members
                    .AsNoTracking()
                    .Where(m =>
                        m.StudentID.Contains(q) ||
                        m.FullName.Contains(q) ||
                        m.Course.Contains(q))
                    .OrderBy(m => m.FullName)
                    .Take(MaxPerType)
                    .ToListAsync();

                model.Activities = await _context.Activities
                    .AsNoTracking()
                    .Where(a =>
                        a.ActivityTitle.Contains(q) ||
                        a.Location.Contains(q))
                    .OrderByDescending(a => a.ActivityDate)
                    .Take(MaxPerType)
                    .ToListAsync();
            }

            return View(model);
        }
    }
}
