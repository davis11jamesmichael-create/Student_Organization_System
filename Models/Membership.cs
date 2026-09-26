using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentOrganizationSystem.Models
{
    public class Membership
    {
        [Key]
        public int MembershipID { get; set; }

        [Required]
        public int MemberID { get; set; }

        [Required]
        public DateTime DateJoined { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Active";

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("MemberID")]
        public Member? Member { get; set; }
    }
}