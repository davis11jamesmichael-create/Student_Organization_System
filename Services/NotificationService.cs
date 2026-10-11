using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Data;
using StudentOrganizationSystem.Models;

namespace StudentOrganizationSystem.Services
{
    /// <summary>
    /// Creates in-app notifications. Called after a successful save in controllers
    /// that trigger events (new announcement, activity cancelled, account approved).
    /// </summary>
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Delivers a notification to a single user.
        /// </summary>
        public async Task NotifyAsync(
            int userId,
            string title,
            string message,
            string category = NotificationCategory.General,
            string? link = null)
        {
            _context.Notifications.Add(new Notification
            {
                UserID = userId,
                Title = title,
                Message = message,
                Category = category,
                Link = link,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Delivers the same notification to every active user whose role matches
        /// the announcement audience. Called after a new announcement is posted.
        /// </summary>
        public async Task NotifyAnnouncementAsync(
            int announcementId,
            string announcementTitle,
            string audience,
            int? authorUserId)
        {
            var query = _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.IsActive);

            // Audience filter mirrors AnnouncementController.Index
            if (audience == AnnouncementAudience.Members)
                query = query.Where(u => u.Role!.RoleName == AppRoles.Member);
            else if (audience == AnnouncementAudience.Staff)
                query = query.Where(u => u.Role!.RoleName == AppRoles.Admin ||
                                         u.Role!.RoleName == AppRoles.Officer);
            // AnnouncementAudience.All → no extra filter

            var recipients = await query.Select(u => u.UserID).ToListAsync();

            var link = $"/Announcement/Details/{announcementId}";
            var now = DateTime.Now;

            foreach (var uid in recipients)
            {
                if (uid == authorUserId) continue; // don't notify the author

                _context.Notifications.Add(new Notification
                {
                    UserID = uid,
                    Title = "New Announcement",
                    Message = announcementTitle,
                    Category = NotificationCategory.Announcement,
                    Link = link,
                    CreatedAt = now
                });
            }

            if (recipients.Any(uid => uid != authorUserId))
                await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Returns the number of unread notifications for a user. Used by the
        /// <c>NotificationBellViewComponent</c> to render the badge.
        /// </summary>
        public async Task<int> UnreadCountAsync(int userId) =>
            await _context.Notifications
                .AsNoTracking()
                .CountAsync(n => n.UserID == userId && !n.IsRead);
    }
}
