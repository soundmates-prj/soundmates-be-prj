using AccountContentService.Application.DTOs;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using AccountContentService.Domain.Entities;
using AutoMapper;
using MediatR;

namespace AccountContentService.Application.Features.BlogPostReactions.Commands.CreateReaction
{
    public class CreateReactionHandler : IRequestHandler<CreateReactionCommand, ReactionDto>
    {
        private readonly IPostReactionRepository _repository;
        private readonly IUserProfileCache _userProfileCache;
        private readonly IMapper _mapper;

        public CreateReactionHandler(
            IPostReactionRepository repository,
            IUserProfileCache userProfileCache,
            IMapper mapper)
        {
            _repository = repository;
            _userProfileCache = userProfileCache;
            _mapper = mapper;
        }

        public async Task<ReactionDto> Handle(
            CreateReactionCommand request,
            CancellationToken cancellationToken)
        {
            var userProfile = await _userProfileCache.GetProfileAsync(request.UserId, cancellationToken);

            var reaction = _mapper.Map<PostReaction>(request);
            reaction.CreatedAt = DateTime.UtcNow;
            reaction.UserAvatarUrl = userProfile.AvatarUrl ?? string.Empty;
            reaction.UserFullName = userProfile.FullName;

            await _repository.AddAsync(reaction);

            return _mapper.Map<ReactionDto>(reaction);
        }
    }
}
