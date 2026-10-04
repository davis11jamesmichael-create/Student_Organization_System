using System;
using System.Collections.Generic;
using System.Linq;

namespace StudentOrganizationSystem.Models
{
    /// <summary>
    /// One row of a member's combined attendance + participation history. Both
    /// records are keyed by the same activity, so a single row carries the
    /// participation type and the attendance status side by side - the only two
    /// columns the old separate tables differed by (module 4a).
    /// </summary>
    public class MemberRecordViewModel
    {
        public int ActivityID { get; set; }

        public string ActivityTitle { get; set; } = string.Empty;

        public DateTime ActivityDate { get; set; }

        public string Location { get; set; } = string.Empty;

        /// <summary>The participation type, when a participation row exists.</summary>
        public string? ParticipationType { get; set; }

        /// <summary>The attendance status (Present / Absent), when one is recorded.</summary>
        public string? AttendanceStatus { get; set; }

        /// <summary>The most recent time either record was saved.</summary>
        public DateTime? RecordedAt { get; set; }
    }

    /// <summary>
    /// Turns a member's separate attendance and participation rows into the single
    /// history the Portal shows. Pure, so it is unit-tested without a database.
    /// </summary>
    public static class MemberRecords
    {
        public static List<MemberRecordViewModel> Combine(
            IEnumerable<Attendance> attendances,
            IEnumerable<Participation> participations)
        {
            var attendanceByActivity = new Dictionary<int, Attendance>();

            foreach (var attendance in attendances)
            {
                attendanceByActivity[attendance.ActivityID] = attendance;
            }

            var participationByActivity = new Dictionary<int, Participation>();

            foreach (var participation in participations)
            {
                participationByActivity[participation.ActivityID] = participation;
            }

            var activityIds = attendanceByActivity.Keys
                .Concat(participationByActivity.Keys)
                .Distinct();

            var rows = new List<MemberRecordViewModel>();

            foreach (var activityId in activityIds)
            {
                attendanceByActivity.TryGetValue(activityId, out var attendance);
                participationByActivity.TryGetValue(activityId, out var participation);

                var activity = attendance?.Activity ?? participation?.Activity;

                // "Recorded" is the later of the two saves, so the row shows when
                // the member's record last changed.
                DateTime? recorded = attendance?.RecordedAt;

                if (participation != null &&
                    (recorded == null || participation.RecordedAt > recorded))
                {
                    recorded = participation.RecordedAt;
                }

                rows.Add(new MemberRecordViewModel
                {
                    ActivityID = activityId,
                    ActivityTitle = activity?.ActivityTitle ?? "(removed activity)",
                    ActivityDate = activity?.ActivityDate ?? default,
                    Location = activity?.Location ?? string.Empty,
                    ParticipationType = participation?.ParticipationType,
                    AttendanceStatus = attendance?.Status,
                    RecordedAt = recorded
                });
            }

            // Newest activity first, the same feel as the old two lists.
            return rows
                .OrderByDescending(r => r.ActivityDate)
                .ThenByDescending(r => r.ActivityTitle)
                .ToList();
        }
    }
}
