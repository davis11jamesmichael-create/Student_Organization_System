using System.ComponentModel.DataAnnotations;

namespace StudentOrganizationSystem.Models
{
    /// <summary>
    /// An organization activity or event. The status shown in the list is not a
    /// stored column: it is derived from <see cref="ActivityDate"/>,
    /// <see cref="StartTime"/>, <see cref="EndTime"/> and the manual
    /// <see cref="IsActive"/> (cancel) flag - see <see cref="ActivityStatus"/>.
    /// </summary>
    public class Activity : IValidatableObject
    {
        [Key]
        public int ActivityID { get; set; }

        [Required]
        [StringLength(150)]
        public string ActivityTitle { get; set; } = string.Empty;

        [Required]
        public DateTime ActivityDate { get; set; }

        /// <summary>
        /// The time the activity starts on <see cref="ActivityDate"/>. Together
        /// with <see cref="EndTime"/> it drives the derived status, so an activity
        /// moves from Upcoming to Ongoing to Finished on its own.
        /// </summary>
        [Display(Name = "Start Time")]
        [DisplayFormat(DataFormatString = "{0:hh\\:mm}", ApplyFormatInEditMode = true)]
        public TimeSpan StartTime { get; set; } = new TimeSpan(9, 0, 0);

        /// <summary>The time the activity ends on <see cref="ActivityDate"/>.</summary>
        [Display(Name = "End Time")]
        [DisplayFormat(DataFormatString = "{0:hh\\:mm}", ApplyFormatInEditMode = true)]
        public TimeSpan EndTime { get; set; } = new TimeSpan(17, 0, 0);

        [Required]
        [StringLength(150)]
        public string Location { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>
        /// The faculty member or instructor in charge. Free text, so an adviser or
        /// a guest speaker who has no account in the system can still be named.
        /// </summary>
        [StringLength(150)]
        [Display(Name = "Faculty / Instructor in charge")]
        public string? FacultyInCharge { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<Attendance> Attendances { get; set; }
            = new List<Attendance>();

        public ICollection<Participation> Participations { get; set; }
            = new List<Participation>();

        /// <summary>
        /// The schedule must make sense: an activity cannot end before it starts.
        /// Running through <see cref="IValidatableObject"/> means the controllers'
        /// <c>TryValidateModel</c> call surfaces the message on the End Time field.
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndTime <= StartTime)
            {
                yield return new ValidationResult(
                    "The end time must be after the start time.",
                    new[] { nameof(EndTime) });
            }
        }
    }
}