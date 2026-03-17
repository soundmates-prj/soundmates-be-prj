namespace AccountContentService.Api.Common
{
    public class PaginationRequest
    {
        private const int MaxPageSize = 50;

        public int Page { get; set; } = 1;

        private int _pageSize = 10;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        // FILTERS
        public string? Status { get; set; }

        public string? MoodTag { get; set; }

        public string? AuthorName { get; set; }

        public string? Search { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }

    public class PaginationNoFilterRequest
    {
        private const int MaxPageSize = 50;

        public int Page { get; set; } = 1;

        private int _pageSize = 10;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }
    }

    public class PaginationResponse<T>
    {
        public IEnumerable<T> Items { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public int TotalCount { get; set; }

        public int TotalPages =>
            (int)Math.Ceiling((double)TotalCount / PageSize);
        public PaginationResponse(
        IEnumerable<T> items,
        int page,
        int pageSize,
        int totalCount)
        {
            Items = items;
            Page = page;
            PageSize = pageSize;
            TotalCount = totalCount;

            TotalCount = totalCount;
        }
    }
}
