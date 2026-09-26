using System.ComponentModel.DataAnnotations;

namespace StudentOrganizationSystem.Models
{
    public class Member
    {
        [Key]
        public int MemberID { get; set; }

        [Required]
        [StringLength(30)]
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

        public ICollection<Membership> Memberships { get; set; }
            = new List<Membership>();

        public ICollection<Attendance> Attendances { get; set; }
            = new List<Attendance>();

        public ICollection<Participation> Participations { get; set; }
            = new List<Participation>();
    }
}