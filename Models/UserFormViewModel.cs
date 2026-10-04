using System.ComponentModel.DataAnnotations;

namespace StudentOrganizationSystem.Models
{
    /// <summary>
    /// The form an Admin uses to create or edit a login account. Password and
    /// ConfirmPassword are optional on edit (blank means "leave it alone"), which
    /// the controller enforces.
    /// </summary>
    public class UserFormViewModel
    {
        public int UserID { get; set; }

        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, MinimumLength = 4,
            ErrorMessage = "Username must be 4 to 50 characters.")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6,
            ErrorMessage = "Password must be at least 6 characters.")]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Compare(nameof(Password),
            ErrorMessage = "The passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Please choose a role.")]
        [Display(Name = "Role")]
        public int RoleID { get; set; }

        [Display(Name = "Linked Member")]
        public int? MemberID { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}