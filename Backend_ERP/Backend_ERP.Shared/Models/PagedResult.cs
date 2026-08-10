namespace ERP.Shared.Models
{
    /// <summary>
    /// Generic paginated result wrapper used by list endpoints.
    /// All list APIs that support pagination should return this type.
    /// </summary>
    public class PagedResult<T>
    {
        /// <summary>Items on the current page.</summary>
        public IReadOnlyList<T> Items { get; init; } = [];

        /// <summary>Total number of matching records (across all pages).</summary>
        public int TotalCount { get; init; }

        /// <summary>Current page number (1-based).</summary>
        public int Page { get; init; }

        /// <summary>Maximum number of records per page.</summary>
        public int PageSize { get; init; }

        /// <summary>Total number of pages.</summary>
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

        /// <summary>Whether a next page exists.</summary>
        public bool HasNextPage => Page < TotalPages;

        /// <summary>Whether a previous page exists.</summary>
        public bool HasPreviousPage => Page > 1;

        /// <summary>
        /// Creates a PagedResult from a full list by applying in-process pagination.
        /// Prefer server-side pagination (SQL OFFSET/LIMIT) in repositories where possible.
        /// </summary>
        public static PagedResult<T> From(IReadOnlyList<T> allItems, int page, int pageSize)
        {
            var safePage = Math.Max(1, page);
            var safeSize = Math.Clamp(pageSize, 1, 1000);
            var items = allItems
                .Skip((safePage - 1) * safeSize)
                .Take(safeSize)
                .ToList();

            return new PagedResult<T>
            {
                Items = items,
                TotalCount = allItems.Count,
                Page = safePage,
                PageSize = safeSize
            };
        }

        /// <summary>Creates a PagedResult from an already-sliced page of items.</summary>
        public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, int page, int pageSize) =>
            new()
            {
                Items = items,
                TotalCount = totalCount,
                Page = Math.Max(1, page),
                PageSize = Math.Max(1, pageSize)
            };
    }
}
