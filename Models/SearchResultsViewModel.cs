using System.Collections.Generic;

namespace StudentOrganizationSystem.Models
{
    /// <summary>
    /// The result of one cross-module search (module 3J): matching members and
    /// matching activities, capped so a broad term cannot return the whole database.
    /// </summary>
    public class SearchResultsViewModel
    {
        public string Query { get; set; } = string.Empty;

        public List<Member> Members { get; set; } = new List<Member>();

        public List<Activity> Activities { get; set; } = new List<Activity>();

        public int Total => Members.Count + Activities.Count;

        public bool HasQuery => !string.IsNullOrWhiteSpace(Query);
    }
}
