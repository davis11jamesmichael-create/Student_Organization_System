using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentOrganizationSystem.Models
{
    public class Attendance
    {
        [Key]
        public int AttendanceID { get; set; }

        [Required]
        public int ActivityID { get; set; }

        [Required]
        public int MemberID { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = string.Empty;

        public DateTime RecordedAt { get; set; } = DateTime.Now;

        [ForeignKey("ActivityID")]
        public Activity? Activity { get; set; }

        [ForeignKey("MemberID")]
        public Member? Member { get; set; }
    }
}