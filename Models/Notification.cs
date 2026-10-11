using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentOrganizationSystem.Models
{
    /// <summary>
    /// An in-app notification delivered to one user. Soft-deleted via
    /// <see cref="IsRead"/> / <see cref="ReadAt"/>; never physically removed.
    /// </summary>
    public class Notification
    {
        [Key]
        public int NotificationID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Optional local path the bell-panel links to (e.g. "/Announcement/Details/3").
        /// Never an external URL.
        /// </summary>
        [StringLength(300)]
        public string? Link { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = NotificationCategory.General;

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ReadAt { get; set; }

        [ForeignKey("UserID")]
        public User? User { get; set; }
    }
}
