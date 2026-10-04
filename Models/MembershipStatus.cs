using System.Collections.Generic;

namespace StudentOrganizationSystem.Models
{
    /// <summary>
    /// The only values stored in <see cref="Membership.Status"/> (decision D2).
    ///
    /// "Inactive" is system-managed: it mirrors <see cref="Member.IsActive"/> being
    /// false, so the two fields can never contradict each other. "Suspended" is the
    /// one standing the active flag cannot express - a member who is still on the
    /// roster but is not in good standing.
    /// </summary>
    public static class MembershipStatus
    {
        public const string Active = "Active";

        public const string Suspended = "Suspended";

        public const string Inactive = "Inactive";

        /// <summary>
        /// The standings an Admin may set by hand. An inactive member is forced to
        /// "Inactive" by the system instead (see MembershipPolicy).
        /// </summary>
        public static readonly IReadOnlyList<string> Selectable =
            new[] { Active, Suspended };

        public static readonly IReadOnlyList<string> All =
            new[] { Active, Suspended, Inactive };

        public static bool IsValid(string? status)
        {
            return status == Active
                || status == Suspended
                || status == Inactive;
        }
    }
}
