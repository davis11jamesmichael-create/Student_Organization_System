using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentOrganizationSystem.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Web-root-relative path to the user's uploaded profile picture, e.g.
        /// "/uploads/avatars/3_ab12.png". Null until a picture is uploaded; the
        /// top bar then falls back to the initial letter. Written by
        /// ProfileController and carried in the sign-in cookie (AppClaims).
        /// </summary>
        [StringLength(255)]
        [Display(Name = "Profile picture")]
        public string? AvatarPath { get; set; }

        [Required]
        public int RoleID { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// C2: the value the sign-in cookie carries and that every request is
        /// checked against. Replacing it (a role change, a deactivation, a password
        /// reset) invalidates every cookie that still holds the old one, so old
        /// privileges cannot outlive the change by up to eight hours.
        /// </summary>
        [Required]
        [StringLength(64)]
        public string SecurityStamp { get; set; } = SecurityStamps.New();

        /// <summary>
        /// C1: true while the account still holds a password somebody else chose -
        /// the seeded first administrator is the only one today. The sign-in cookie
        /// carries the flag (<see cref="AppClaims.MustChangePassword"/>) and no page
        /// other than the change-password form opens until it is cleared.
        /// </summary>
        public bool MustChangePassword { get; set; }

        /// <summary>
        /// C6: the seeded first administrator is the super-admin. A normal Admin
        /// cannot edit, demote, deactivate or reset the password of another Admin,
        /// and the role dropdown hides the Admin option for non-super-admins.
        /// </summary>
        public bool IsSuperAdmin { get; set; }

        /// <summary>
        /// C7: set on every successful sign-in so dormant accounts can be spotted
        /// and deactivated by an Admin action.
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// Set only for "Member" accounts: the Members row this login belongs to.
        /// Null for Admin and Officer accounts, which manage the whole system
        /// rather than one member. The one-to-one relationship gives a unique
        /// index, so one member can never have two accounts.
        /// </summary>
        public int? MemberID { get; set; }

        [ForeignKey("RoleID")]
        public Role? Role { get; set; }

        [ForeignKey("MemberID")]
        public Member? Member { get; set; }
    }
}