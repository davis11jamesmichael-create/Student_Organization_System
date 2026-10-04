using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Models;

namespace StudentOrganizationSystem.Data
{
    /// <summary>
    /// Shared list paging (AUDIT.md P3). One place decides the page size and how a
    /// requested page is clamped, so every list behaves the same way.
    /// </summary>
    public static class Paging
    {
        public const int DefaultPageSize = 20;

        public static async Task<PagedResult<T>> ToPagedAsync<T>(
            IQueryable<T> query,
            int page,
            int pageSize = DefaultPageSize)
        {
            var totalCount = await query.CountAsync();

            var totalPages = totalCount == 0
                ? 1
                : (int)Math.Ceiling(totalCount / (double)pageSize);

            // A hand-edited ?page=999 must not produce an empty page.
            var current = Math.Clamp(page, 1, totalPages);

            var items = await query
                .Skip((current - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<T>
            {
                Items = items,
                Page = current,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        /// <summary>
        /// The same windowing over an already materialised list. Used when the list
        /// has a filter that cannot be expressed in SQL - the Activities status is
        /// derived from the clock, so those rows are loaded once and then filtered
        /// and paged here.
        /// </summary>
        public static PagedResult<T> ToPaged<T>(
            IReadOnlyList<T> items,
            int page,
            int pageSize = DefaultPageSize)
        {
            var totalCount = items.Count;

            var totalPages = totalCount == 0
                ? 1
                : (int)Math.Ceiling(totalCount / (double)pageSize);

            var current = Math.Clamp(page, 1, totalPages);

            var window = items
                .Skip((current - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<T>
            {
                Items = window,
                Page = current,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
    }
}
