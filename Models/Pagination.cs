using System;
using System.Collections.Generic;

namespace StudentOrganizationSystem.Models
{
    /// <summary>
    /// One page of a list plus the counters a pager needs (AUDIT.md P3).
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public int TotalCount { get; set; }

        public int TotalPages =>
            TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

        public bool HasPrevious => Page > 1;

        public bool HasNext => Page < TotalPages;

        /// <summary>1-based number of the first item on this page (0 when empty).</summary>
        public int FirstItem => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;

        public int LastItem => Math.Min(Page * PageSize, TotalCount);

        /// <summary>
        /// Builds the small view model the shared _Pager partial renders. The
        /// current filters are carried in <paramref name="routeValues"/> so a page
        /// link never silently drops the search or status the user chose.
        /// </summary>
        public PagerViewModel ToPager(
            string controller,
            string action,
            IDictionary<string, string>? routeValues = null)
        {
            var cleaned = new Dictionary<string, string>();

            if (routeValues != null)
            {
                foreach (var pair in routeValues)
                {
                    if (!string.IsNullOrEmpty(pair.Value))
                    {
                        cleaned[pair.Key] = pair.Value;
                    }
                }
            }

            return new PagerViewModel
            {
                Controller = controller,
                Action = action,
                Page = Page,
                TotalPages = TotalPages,
                TotalCount = TotalCount,
                PageSize = PageSize,
                FirstItem = FirstItem,
                LastItem = LastItem,
                HasPrevious = HasPrevious,
                HasNext = HasNext,
                RouteValues = cleaned
            };
        }
    }

    /// <summary>What the shared _Pager partial binds to.</summary>
    public class PagerViewModel
    {
        public string Controller { get; set; } = string.Empty;

        public string Action { get; set; } = "Index";

        public int Page { get; set; } = 1;

        public int TotalPages { get; set; } = 1;

        public int TotalCount { get; set; }

        public int PageSize { get; set; } = 20;

        public int FirstItem { get; set; }

        public int LastItem { get; set; }

        public bool HasPrevious { get; set; }

        public bool HasNext { get; set; }

        /// <summary>The current filters, so a page link keeps them.</summary>
        public Dictionary<string, string> RouteValues { get; set; }
            = new Dictionary<string, string>();
    }
}
