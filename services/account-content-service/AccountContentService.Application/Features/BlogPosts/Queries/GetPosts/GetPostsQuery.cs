using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.DTOs;
using AccountContentService.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPosts.Queries.GetPosts
{
    public class GetPostsQuery : IRequest<PaginationResult<PostDto>>
    {
        public int Page { get; set; }

        public int PageSize { get; set; }

        public string? Status { get; set; }

        public string? MoodTag { get; set; }

        public string? AuthorName { get; set; }

        public string? Search { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

    }

    public class GetPublisedPostsQuery : IRequest<PaginationResult<PostDto>>
    {
        public int Page { get; set; }

        public int PageSize { get; set; }

        public string? MoodTag { get; set; }

        public string? AuthorName { get; set; }

        public string? Search { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }



    public class GetPostDetailQuery : IRequest<PostDto>
    {
        public Guid PostId { get; set; }

        public GetPostDetailQuery(Guid postId)
        {
            PostId = postId;
        }
    }

    public class GetPublisedPostDetailQuery : IRequest<PostDto>
    {
        public Guid PostId { get; set; }

        public GetPublisedPostDetailQuery(Guid postId)
        {
            PostId = postId;
        }
    }

    public class GetUserPostDetailQuery : IRequest<PaginationResult<PostDto>>
    {
        public Guid UserId { get; set; }
        public int Page { get; set; }

        public int PageSize { get; set; }

        public GetUserPostDetailQuery(Guid userId, int page, int pageSize)
        {
            UserId = userId;
            Page = page;
            PageSize = pageSize;
        }
    }

    public class GetCurrentUserPostDetailQuery : IRequest<PaginationResult<PostDto>>
    {
        public Guid UserId { get; set; }
        public int Page { get; set; }

        public int PageSize { get; set; }

        public GetCurrentUserPostDetailQuery(Guid userId, int page, int pageSize)
        {
            UserId = userId;
            Page = page;
            PageSize = pageSize;
        }
    }

}
