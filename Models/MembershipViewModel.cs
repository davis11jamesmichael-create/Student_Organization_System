using System.ComponentModel.DataAnnotations;

namespace StudentOrganizationSystem.Models
{
    /// <summary>
    /// One row of the Membership list (module 3G), and the backing model for the
    /// standing form. The member fields are display-only: only MemberID and Status
    /// are posted, so a crafted request cannot move a membership to another member
    /// (the same mass-assignment rule the rest of the project follows).
    /// </summary>
    public class MembershipViewModel
    {
        public int MemberID { get; set; }

        [Display(Name = "Student ID")]
        public string StudentID { get; set; } = string.Empty;

        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        public string Course { get; set; } = string.Empty;

        public string YearLevel { get; set; } = string.Empty;

        /// <summary>
        /// The member's active flag. It is the source of truth (decision D2): a
        /// deactivated member can never hold an "Active" or "Suspended" standing.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>Null when the member has no membership row yet.</summary>
        public int? MembershipID { get; set; }

        [Display(Name = "Date Joined")]
        public DateTime? DateJoined { get; set; }

        [Required(ErrorMessage = "Please choose a standing.")]
        [Display(Name = "Standing")]
        public string Status { get; set; } = MembershipStatus.Active;

        public DateTime? UpdatedAt { get; set; }

        /// <summary>True when no membership row exists yet (the form acts as "Add").</summary>
        public bool IsNew => MembershipID == null;
    }
}
