using System.ComponentModel.DataAnnotations;

namespace StudentOrganizationSystem.Models
{
    public class Activity
    {
        [Key]
        public int ActivityID { get; set; }

        [Required]
        [StringLength(150)]
        public string ActivityTitle { get; set; } = string.Empty;

        [Required]
        public DateTime ActivityDate { get; set; }

        [Required]
        [StringLength(150)]
        public string Location { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<Attendance> Attendances { get; set; }
            = new List<Attendance>();

        public ICollection<Participation> Participations { get; set; }
            = new List<Participation>();
    }
}