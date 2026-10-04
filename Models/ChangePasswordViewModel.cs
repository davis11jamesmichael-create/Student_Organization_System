using System.ComponentModel.DataAnnotations;

namespace StudentOrganizationSystem.Models
{
    /// <summary>
    /// Self-service change-password form (AUDIT.md P1.9). Kept separate from the
    /// User entity so the plain password is never part of a bound model.
    /// </summary>
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Your current password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "A new password is required.")]
        [StringLength(100, MinimumLength = 6,
            ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm the new password.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword),
            ErrorMessage = "The passwords do not match.")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}