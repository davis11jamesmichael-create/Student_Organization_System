using System.ComponentModel.DataAnnotations;

namespace StudentOrganizationSystem.Models
{
    /// <summary>
    /// The public self-registration form. Deliberately separate from Member and
    /// User so a crafted POST cannot set IsActive, RoleID or MemberID: those are
    /// decided by AccountController.Register, never by the browser (the same
    /// mass-assignment rule the Phase 0 review applied to the other controllers).
    /// </summary>
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, MinimumLength = 4,
            ErrorMessage = "Username must be 4 to 50 characters.")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6,
            ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password),
            ErrorMessage = "The passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Student ID is required.")]
        [StringLength(30)]
        [RegularExpression(@"^\d{4}-\d{5}$",
            ErrorMessage = "Student ID must look like 2024-00001.")]
        [Display(Name = "Student ID")]
        public string StudentID { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(150)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Course is required.")]
        [StringLength(100)]
        [Display(Name = "Course")]
        public string Course { get; set; } = string.Empty;

        [Required(ErrorMessage = "Year level is required.")]
        [StringLength(30)]
        [Display(Name = "Year Level")]
        public string YearLevel { get; set; } = string.Empty;
    }
}