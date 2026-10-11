namespace StudentOrganizationSystem.Models
{
    /// <summary>
    /// Allowed values for <see cref="Notification.Category"/>. Shared constants
    /// so the service, controller and tests never drift apart.
    /// </summary>
    public static class NotificationCategory
    {
        public const string General = "General";
        public const string Announcement = "Announcement";
        public const string Activity = "Activity";
        public const string Account = "Account";

        public static bool IsValid(string? value) =>
            value == General ||
            value == Announcement ||
            value == Activity ||
            value == Account;
    }
}
