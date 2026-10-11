namespace StudentOrganizationSystem.Models
{
    /// <summary>
    /// The allowed values for <see cref="Announcement.Audience"/>. Shared constants
    /// so the controller, views and tests never drift apart.
    /// </summary>
    public static class AnnouncementAudience
    {
        public const string All = "All";
        public const string Members = "Members";
        public const string Staff = "Staff";

        public static readonly string[] All_Values = { All, Members, Staff };

        public static bool IsValid(string? value) =>
            value == All || value == Members || value == Staff;
    }
}
