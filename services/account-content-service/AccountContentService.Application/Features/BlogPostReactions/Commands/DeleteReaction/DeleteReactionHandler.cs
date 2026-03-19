using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Features.BlogComments.Commands.DeleteComment;
using AccountContentService.Application.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPostReactions.Commands.DeleteReaction
{
    public class DeleteReactionHandler : IRequestHandler<DeleteReactionCommand, bool>
    {
        private readonly IPostReactionRepository _repository;
        
        public DeleteReactionHandler(IPostReactionRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(
            DeleteReactionCommand request,
            CancellationToken cancellationToken)
        {
            var reaction = await _repository.GetUserReactionAsync(request.UserId, request.PostId, cancellationToken);

            if (reaction == null)
            {
                throw new NotFoundException("Reaction not found");
            }

            await _repository.DeleteAsync(reaction);

            return true;
        }
    }
}
