using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentOrganizationSystem.Models
{
    /// <summary>
    /// An organization-wide announcement posted by staff. The audience controls
    /// who sees it: All (everyone), Members (Member role only), Staff (Admin/Officer).
    /// Soft-deleted via <see cref="IsActive"/>; pinned rows float to the top.
    /// </summary>
    public class Announcement
    {
        [Key]
        public int AnnouncementID { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Who can read this announcement: <c>All</c>, <c>Members</c>, or <c>Staff</c>.
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Audience { get; set; } = AnnouncementAudience.All;

        public bool IsPinned { get; set; }

        public bool IsActive { get; set; } = true;

        public int? CreatedByUserID { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("CreatedByUserID")]
        public User? CreatedByUser { get; set; }
    }
}
