using System.Collections.Generic;

namespace StudentOrganizationSystem.Models
{
    /// <summary>
    /// The only values that may be stored in <see cref="Attendance.Status"/>.
    /// Shared by the controller (the server-side whitelist, AUDIT.md P1.2) and
    /// the views, so the two can never drift apart - the same idea as
    /// <see cref="AppRoles"/>.
    /// </summary>
    public static class AttendanceStatus
    {
        public const string Present = "Present";

        public const string Absent = "Absent";

        /// <summary>Every status a form is allowed to submit.</summary>
        public static readonly IReadOnlyList<string> All =
            new[] { Present, Absent };

        /// <summary>
        /// True only for a known status. Anything else (a typo, a stale form or a
        /// hand-crafted POST) is rejected instead of being stored as free text.
        /// </summary>
        public static bool IsValid(string? status)
        {
            return status == Present || status == Absent;
        }
    }
}
