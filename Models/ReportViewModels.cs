using System;

namespace StudentOrganizationSystem.Models
{
    /// <summary>
    /// One row of the per-activity attendance summary (module 3I).
    /// <see cref="Total"/> and <see cref="Rate"/> are computed, not stored.
    /// </summary>
    public class ActivityAttendanceReportRow
    {
        public int ActivityID { get; set; }

        public string ActivityTitle { get; set; } = string.Empty;

        public DateTime ActivityDate { get; set; }

        public bool IsActive { get; set; }

        public int Present { get; set; }

        public int Absent { get; set; }

        public int Total => Present + Absent;

        public double Rate => Total == 0 ? 0 : (double)Present / Total * 100;
    }

    /// <summary>
    /// One row of the per-member attendance and participation summary (module 3I).
    /// </summary>
    public class MemberReportRow
    {
        public int MemberID { get; set; }

        public string StudentID { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        /// <summary>The membership standing, or empty when none is set up (D2).</summary>
        public string Standing { get; set; } = string.Empty;

        public int Present { get; set; }

        public int Absent { get; set; }

        public int ParticipationCount { get; set; }

        public int Total => Present + Absent;

        public double Rate => Total == 0 ? 0 : (double)Present / Total * 100;
    }
}
