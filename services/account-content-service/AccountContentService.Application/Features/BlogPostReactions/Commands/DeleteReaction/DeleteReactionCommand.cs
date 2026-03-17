using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPostReactions.Commands.DeleteReaction
{
    public class DeleteReactionCommand : IRequest<bool>
    {
        public Guid PostId { get; set; }
        public Guid UserId { get; set; }

        public DeleteReactionCommand(Guid postId, Guid userId)
        {
            PostId = postId;
            UserId = userId;
        }
    }
}
