using AccountContentService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPostReactions.Queries.GetReactions
{
    public class GetReactionsQuery : IRequest<List<ReactionDto>>
    {
        public Guid PostId { get; set; }
        public GetReactionsQuery(Guid postId)
        {
            PostId = postId;
        }
    }
}
