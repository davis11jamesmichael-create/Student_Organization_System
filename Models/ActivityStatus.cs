using System;
using System.Collections.Generic;

namespace StudentOrganizationSystem.Models
{
    /// <summary>
    /// The status shown for an activity in the list. Unlike
    /// <see cref="MembershipStatus"/> (stored) these values are derived, not saved:
    /// a cancelled activity is always "Cancelled", and every other activity is
    /// categorised from the clock and its schedule. The badge therefore stays
    /// correct the moment a page is rendered, with nothing to keep in sync.
    /// </summary>
    public static class ActivityStatus
    {
        public const string Upcoming = "Upcoming";

        public const string Ongoing = "Ongoing";

        public const string Cancelled = "Cancelled";

        public const string Finished = "Finished";

        /// <summary>The values an Admin can pick in the list filter.</summary>
        public static readonly IReadOnlyList<string> Filterable =
            new[] { Upcoming, Ongoing, Cancelled, Finished };

        /// <summary>The schedule's start as a single instant.</summary>
        public static DateTime StartOf(Activity activity)
        {
            return activity.ActivityDate.Date.Add(activity.StartTime);
        }

        /// <summary>The schedule's end as a single instant.</summary>
        public static DateTime EndOf(Activity activity)
        {
            return activity.ActivityDate.Date.Add(activity.EndTime);
        }

        /// <summary>
        /// Derives the status: a cancelled (deactivated) activity is always
        /// "Cancelled"; otherwise the clock decides. <paramref name="now"/> is
        /// injectable so the rule can be tested without waiting for time to pass.
        /// </summary>
        public static string For(Activity activity, DateTime? now = null)
        {
            if (!activity.IsActive)
            {
                return Cancelled;
            }

            var moment = now ?? DateTime.Now;

            if (moment < StartOf(activity))
            {
                return Upcoming;
            }

            // Between the start and the end, inclusive, the activity is running.
            if (moment <= EndOf(activity))
            {
                return Ongoing;
            }

            return Finished;
        }

        /// <summary>True only for a value the filter recognises.</summary>
        public static bool IsFilterable(string? status)
        {
            return status == Upcoming
                || status == Ongoing
                || status == Cancelled
                || status == Finished;
        }

        /// <summary>
        /// The CSS badge modifier for a status, matching the existing
        /// <c>badge-*</c> classes in components.css.
        /// </summary>
        public static string BadgeClass(string status)
        {
            return status switch
            {
                Upcoming => "badge-upcoming",
                Ongoing => "badge-ongoing",
                Cancelled => "badge-cancelled",
                _ => "badge-finished"
            };
        }
    }
}
