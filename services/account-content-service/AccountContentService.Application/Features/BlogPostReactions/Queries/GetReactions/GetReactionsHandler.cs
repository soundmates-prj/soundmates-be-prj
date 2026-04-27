using AccountContentService.Application.DTOs;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
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
        private readonly IUserProfileCache _userProfileCache;
        private readonly IMapper _mapper;

        public GetReactionsHandler(
            IPostReactionRepository repository,
            IUserProfileCache userProfileCache,
            IMapper mapper)
        {
            _repository = repository;
            _userProfileCache = userProfileCache;
            _mapper = mapper;
        }

        public async Task<List<ReactionDto>> Handle(GetReactionsQuery request, CancellationToken cancellationToken)
        {
            var reactions = await _repository.GetByPostIdAsync(request.PostId, cancellationToken);
            var items = _mapper.Map<List<ReactionDto>>(reactions);

            foreach (var reaction in items)
            {
                await PopulateUserProfileAsync(reaction, cancellationToken);
            }

            return items;
        }

        /// <summary>
        /// Refreshes userFullName and userAvatarUrl from the local read-model
        /// projection.
        /// </summary>
        private async Task PopulateUserProfileAsync(ReactionDto reaction, CancellationToken ct)
        {
            if (reaction.UserId == Guid.Empty) return;
            var profile = await _userProfileCache.GetProfileAsync(reaction.UserId, ct);
            reaction.UserFullName = profile.FullName;
            reaction.UserAvatarUrl = profile.AvatarUrl ?? string.Empty;
        }
    }
}
