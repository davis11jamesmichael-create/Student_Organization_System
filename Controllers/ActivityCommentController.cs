using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Data;
using StudentOrganizationSystem.Models;

namespace StudentOrganizationSystem.Controllers
{
    // Every signed-in user (Admin, Officer, Member) may read and post comments.
    // Edit and Delete are further guarded to author-or-Admin inside each action.
    [Authorize]
    public class ActivityCommentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ActivityCommentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: /ActivityComment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int activityId, string body)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Challenge();

            body = (body ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(body) || body.Length > 1000)
            {
                TempData["Error"] = "Comment must be between 1 and 1000 characters.";
                return RedirectToComments(activityId);
            }

            // Verify the activity exists and is active.
            var activity = await _context.Activities
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.ActivityID == activityId && a.IsActive);

            if (activity == null) return NotFound();

            _context.ActivityComments.Add(new ActivityComment
            {
                ActivityID = activityId,
                UserID = userId.Value,
                Body = body,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Comment added.";
            return RedirectToComments(activityId);
        }

        // GET: /ActivityComment/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var comment = await _context.ActivityComments
                .AsNoTracking()
                .Include(c => c.Activity)
                .FirstOrDefaultAsync(c => c.CommentID == id && c.IsActive);

            if (comment == null) return NotFound();

            if (!CanModify(comment)) return Forbid();

            return View(comment);
        }

        // POST: /ActivityComment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CommentID,Body")] ActivityComment posted)
        {
            if (id != posted.CommentID) return NotFound();

            var comment = await _context.ActivityComments
                .Include(c => c.Activity)
                .FirstOrDefaultAsync(c => c.CommentID == id && c.IsActive);

            if (comment == null) return NotFound();
            if (!CanModify(comment)) return Forbid();

            posted.Body = (posted.Body ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(posted.Body) || posted.Body.Length > 1000)
            {
                ModelState.AddModelError("Body", "Comment must be between 1 and 1000 characters.");
                return View(comment);
            }

            comment.Body = posted.Body;
            comment.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Comment updated.";
            return RedirectToComments(comment.ActivityID);
        }

        // POST: /ActivityComment/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var comment = await _context.ActivityComments
                .FirstOrDefaultAsync(c => c.CommentID == id && c.IsActive);

            if (comment == null) return NotFound();
            if (!CanModify(comment)) return Forbid();

            comment.IsActive = false;
            comment.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Comment removed.";
            return RedirectToComments(comment.ActivityID);
        }

        // ── helpers ──

        private int? GetCurrentUserId()
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }

        /// <summary>
        /// Author or Admin may edit/delete. Officers cannot touch another user's comment.
        /// </summary>
        private bool CanModify(ActivityComment comment)
        {
            var userId = GetCurrentUserId();
            return User.IsInRole(AppRoles.Admin) || comment.UserID == userId;
        }

        /// <summary>
        /// Staff land back on Activity/Details; Members land on Portal/Activities.
        /// </summary>
        private IActionResult RedirectToComments(int activityId)
        {
            if (User.IsInRole(AppRoles.Member))
                return RedirectToAction("Activities", "Portal", new { id = activityId });

            return RedirectToAction("Details", "Activity", new { id = activityId });
        }
    }
}
