using AccountContentService.Application.Common.Pagination;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPosts.Queries.GetTrendingPosts
{
    public class GetTrendingPostsQuery : IRequest<PaginationResult<TrendingPostResponse>>
    {
        public int Page { get; set; }

        public int PageSize { get; set; }

        public string? MoodTag { get; set; }

        public string? AuthorName { get; set; }

        public string? Search { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }
}
