using AccountContentService.Application.DTOs;
using AccountContentService.Application.Interfaces.Repositories;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPostReactions.Queries.GetReactions
{
    public class GetReactionsHandler : IRequestHandler<GetReactionsQuery, List<ReactionDto>>
    {
        private readonly IPostReactionRepository _repository;
        private readonly IMapper _mapper;

        public GetReactionsHandler(IPostReactionRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<ReactionDto>> Handle(GetReactionsQuery request, CancellationToken cancellationToken)
        {
            var reactions = await _repository.GetByPostIdAsync(request.PostId, cancellationToken);
            return _mapper.Map<List<ReactionDto>>(reactions);
        }
    }
}
