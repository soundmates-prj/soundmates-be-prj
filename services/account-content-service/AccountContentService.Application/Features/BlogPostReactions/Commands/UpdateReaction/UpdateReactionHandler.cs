using AccountContentService.Application.DTOs;
using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Interfaces.Repositories;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPostReactions.Commands.UpdateReaction
{
    public class UpdateReactionHandler : IRequestHandler<UpdateReactionCommand, ReactionDto>
    {
        private readonly IPostReactionRepository _repository;
        private readonly IMapper _mapper;
        public UpdateReactionHandler(IPostReactionRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }
        public async Task<ReactionDto> Handle(UpdateReactionCommand request, CancellationToken cancellationToken)
        {
            var reaction = await _repository.GetByIdAsync(request.ReactionId, cancellationToken);
            if (reaction == null)
            {
                throw new NotFoundException($"Reaction with ID {request.ReactionId} not found.");
            }
            reaction.ReactionType = request.ReactionType;
            reaction.CreatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(reaction);
            return _mapper.Map<ReactionDto>(reaction);
        }
    }
}
