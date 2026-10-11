using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentOrganizationSystem.Models
{
    /// <summary>
    /// An append-only record of every state-changing administrative action.
    /// Never physically deleted; the controller exposes read-only access.
    /// </summary>
    public class AuditLog
    {
        [Key]
        public int AuditLogID { get; set; }

        /// <summary>The user who performed the action. Nullable so system-level
        /// events (seeder, startup) can still be recorded.</summary>
        public int? ActorUserID { get; set; }

        /// <summary>Dot-separated action name, e.g. <c>User.Create</c>.</summary>
        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        /// <summary>The type of entity affected, e.g. <c>User</c>.</summary>
        [Required]
        [StringLength(100)]
        public string EntityName { get; set; } = string.Empty;

        /// <summary>The primary key of the affected row, when applicable.</summary>
        public int? EntityID { get; set; }

        /// <summary>Human-readable summary of what changed.</summary>
        [StringLength(500)]
        public string? Details { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ActorUserID")]
        public User? ActorUser { get; set; }
    }
}
