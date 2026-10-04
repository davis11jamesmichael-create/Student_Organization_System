using System.Collections.Generic;

namespace StudentOrganizationSystem.Models
{
    /// <summary>
    /// Suggested values for the free-text <see cref="Participation.ParticipationType"/>
    /// column (the finalized design keeps this a VARCHAR, not its own table). The
    /// form offers them as a datalist, so typing stays quick without turning the
    /// field into a hard-coded list.
    /// </summary>
    public static class ParticipationTypes
    {
        /// <summary>Matches the column's StringLength(100).</summary>
        public const int MaxLength = 100;

        public static readonly IReadOnlyList<string> Suggested = new[]
        {
            "Organizer",
            "Participant",
            "Volunteer",
            "Committee Member",
            "Facilitator",
            "Speaker",
            "Judge"
        };

        public static string Normalize(string? value)
        {
            return (value ?? string.Empty).Trim();
        }
    }
}
