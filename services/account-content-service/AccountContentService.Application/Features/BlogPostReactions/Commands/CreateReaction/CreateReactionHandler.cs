using AccountContentService.Application.DTOs;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPostReactions.Commands.CreateReaction
{
    public class CreateReactionHandler : IRequestHandler<CreateReactionCommand, ReactionDto>
    {
        private readonly IPostReactionRepository _repository;
        private readonly IMapper _mapper;

        public CreateReactionHandler(IPostReactionRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<ReactionDto> Handle(CreateReactionCommand request, CancellationToken cancellationToken)
        {
            var reaction = _mapper.Map<PostReaction>(request);
            reaction.CreatedAt = DateTime.UtcNow;

            await _repository.AddAsync(reaction);

            return _mapper.Map<ReactionDto>(reaction);
        }
    }
}
