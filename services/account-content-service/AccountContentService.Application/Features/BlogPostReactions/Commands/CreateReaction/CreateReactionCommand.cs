using AccountContentService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPostReactions.Commands.CreateReaction
{
    public class CreateReactionCommand : IRequest<ReactionDto>
    {
        public required Guid PostId { get; set; }

        public required Guid UserId { get; set; }

        public required string ReactionType { get; set; }
    }
}
