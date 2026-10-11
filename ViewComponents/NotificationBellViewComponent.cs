using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using StudentOrganizationSystem.Services;

namespace StudentOrganizationSystem.ViewComponents
{
    /// <summary>
    /// Renders the bell icon + unread badge in the top bar. Using a ViewComponent
    /// keeps the DB call out of _Layout.cshtml and avoids polluting every
    /// controller with a ViewBag assignment.
    /// </summary>
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly NotificationService _notifications;

        public NotificationBellViewComponent(NotificationService notifications)
        {
            _notifications = notifications;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var value = UserClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(value, out var userId))
                return View(0);

            var count = await _notifications.UnreadCountAsync(userId);
            return View(count);
        }
    }
}
