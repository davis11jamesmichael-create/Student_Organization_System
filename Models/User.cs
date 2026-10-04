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