using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPosts.Queries.GetPostStats
{
    public class GetPostStatsQuery : IRequest<PostStatsResponse>
    {
        public Guid PostId { get; set; }

        public GetPostStatsQuery(Guid postId)
        {
            PostId = postId;
        }
    }

    public class GetPostsStatsQuery : IRequest<PaginationResult<PostStatsResponse>>
    {
        public int Page { get; set; }

        public int PageSize { get; set; }

        public string? MoodTag { get; set; }

        public string? AuthorName { get; set; }
        public string? Status { get; set; }

        public string? Search { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }

}
