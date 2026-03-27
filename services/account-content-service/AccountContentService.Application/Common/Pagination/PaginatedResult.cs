using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Common.Pagination
{
    public class PaginationResult<T>
    {
        public IEnumerable<T> Items { get; set; } = [];

        public int TotalCount { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }
    }
}
