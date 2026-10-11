using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentOrganizationSystem.Models
{
    /// <summary>
    /// A comment left by any signed-in user (Admin, Officer, or Member) on an
    /// activity. Soft-deleted via <see cref="IsActive"/>; never physically removed.
    /// </summary>
    public class ActivityComment
    {
        [Key]
        public int CommentID { get; set; }

        [Required]
        public int ActivityID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [StringLength(1000)]
        public string Body { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("ActivityID")]
        public Activity? Activity { get; set; }

        [ForeignKey("UserID")]
        public User? User { get; set; }
    }
}
