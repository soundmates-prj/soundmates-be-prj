using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogComments.Queries.GetComments
{
    public class GetCommentsQuery : IRequest<PaginationResult<CommentDto>>
    {
        public Guid PostId { get; set; }
        public int Page { get; set; }

        public int PageSize { get; set; }
    }

    public class GetUserCommentsQuery : IRequest<PaginationResult<CommentDto>>
    {
        public Guid UserId { get; set; }
        public int Page { get; set; }

        public int PageSize { get; set; }
    }

    public class GetCommentDetailQuery : IRequest<List<CommentDto>>
    {
        public Guid CommentId { get; set; }
        
        public GetCommentDetailQuery(Guid commentId)
        {
            CommentId = commentId;
        }
    }
}
