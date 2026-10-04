using System.ComponentModel.DataAnnotations;

namespace StudentOrganizationSystem.Models
{
    public class Member
    {
        [Key]
        public int MemberID { get; set; }

        [Required]
        [StringLength(30)]
        // P1.6: one canonical format (the same one the public registration form
        // uses). It also rejects a stray trailing space, which the NO PAD MySQL
        // collation would otherwise accept as a second, lookalike Student ID.
        [RegularExpression(@"^\d{4}-\d{5}$",
            ErrorMessage = "Student ID must look like 2024-00001.")]
        [Display(Name = "Student ID")]
        public string StudentID { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Course { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string YearLevel { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// The login account for this member, when one exists. Null until the
        /// member registers (or an Admin links an account); a Member login cannot
        /// exist without the Members row it points at.
        /// </summary>
        public User? User { get; set; }

        public ICollection<Membership> Memberships { get; set; }
            = new List<Membership>();

        public ICollection<Attendance> Attendances { get; set; }
            = new List<Attendance>();

        public ICollection<Participation> Participations { get; set; }
            = new List<Participation>();
    }
}