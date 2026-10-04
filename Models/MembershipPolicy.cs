using System.Linq;

namespace StudentOrganizationSystem.Models
{
    /// <summary>
    /// Keeps <see cref="Membership.Status"/> consistent with
    /// <see cref="Member.IsActive"/> (decision D2). Member.IsActive is the source of
    /// truth: an inactive member's membership is always "Inactive", and a
    /// reactivated member returns to "Active". A hand-set "Suspended" standing is
    /// preserved, because it says something the active flag cannot.
    ///
    /// These helpers work on the Member's loaded <c>Memberships</c> collection, so a
    /// caller must load the member with <c>.Include(m =&gt; m.Memberships)</c>.
    /// </summary>
    public static class MembershipPolicy
    {
        /// <summary>The standing that automatically matches an active flag.</summary>
        public static string StandingFor(bool isActive)
        {
            return isActive ? MembershipStatus.Active : MembershipStatus.Inactive;
        }

        /// <summary>
        /// Returns the member's single membership row, creating (in memory) one that
        /// matches the active flag when the member has none yet. A legacy member
        /// therefore gains a row the first time they are touched.
        /// </summary>
        public static Membership Ensure(Member member)
        {
            var membership = member.Memberships.FirstOrDefault();

            if (membership != null)
            {
                return membership;
            }

            membership = new Membership
            {
                DateJoined = member.CreatedAt == default
                    ? DateTime.Now
                    : member.CreatedAt,

                Status = StandingFor(member.IsActive)
            };

            member.Memberships.Add(membership);

            return membership;
        }

        /// <summary>
        /// Re-derives the standing from the active flag and stamps UpdatedAt. Call
        /// this after changing <see cref="Member.IsActive"/>.
        /// </summary>
        public static void SyncStanding(Member member)
        {
            var membership = Ensure(member);

            // "Suspended" survives an activation, but an inactive member can never
            // keep a non-inactive standing.
            membership.Status = member.IsActive
                ? (membership.Status == MembershipStatus.Inactive
                    ? MembershipStatus.Active
                    : membership.Status)
                : MembershipStatus.Inactive;

            membership.UpdatedAt = DateTime.Now;
        }
    }
}
