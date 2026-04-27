using AccountContentService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPostReactions.Commands.UpdateReaction
{
    public class UpdateReactionCommand : IRequest<ReactionDto>
    {
        public required Guid ReactionId { get; set; }

        public required string ReactionType { get; set; }
    }
}
