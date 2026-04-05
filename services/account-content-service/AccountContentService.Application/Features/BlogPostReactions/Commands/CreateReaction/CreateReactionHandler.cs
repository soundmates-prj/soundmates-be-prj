using AccountContentService.Application.DTOs;
using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using AccountContentService.Domain.Entities;
using AutoMapper;
using MediatR;
using shared.Contracts.Events.Notifications;
using System.Text.Json;
using System.Xml.Linq;

namespace AccountContentService.Application.Features.BlogPostReactions.Commands.CreateReaction
{
    public class CreateReactionHandler : IRequestHandler<CreateReactionCommand, ReactionDto>
    {
        private readonly IPostReactionRepository _repository;
        private readonly IBlogPostRepository _postRepository;
        private readonly IUserProfileCache _userProfileCache;
        private readonly IMapper _mapper;
        private readonly IMessageBusPublisher _eventBus;

        public CreateReactionHandler(
            IPostReactionRepository repository,
            IUserProfileCache userProfileCache,
            IMapper mapper,
            IMessageBusPublisher eventBus,
            IBlogPostRepository postRepository)
        {
            _repository = repository;
            _userProfileCache = userProfileCache;
            _mapper = mapper;
            _eventBus = eventBus;
            _postRepository = postRepository;
        }

        public async Task<ReactionDto> Handle(
            CreateReactionCommand request,
            CancellationToken cancellationToken)
        {
            var post = await _postRepository.GetByIdAsync(request.PostId, cancellationToken);

            if (post == null)
            {
                throw new NotFoundException("Post not found");
            }

            var userProfile = await _userProfileCache.GetProfileAsync(request.UserId, cancellationToken);

            var reaction = _mapper.Map<PostReaction>(request);
            reaction.CreatedAt = DateTime.UtcNow;
            reaction.UserAvatarUrl = userProfile.AvatarUrl ?? string.Empty;
            reaction.UserFullName = userProfile.FullName;

            await _repository.AddAsync(reaction);


            var @event = new NotificationEvent
            {
                Title = "New Reaction",
                ReceiveUserId = post.UserId,
                ReferenceId = reaction.PostId,
                Type = "post-reaction",
                Message = $"{userProfile.FullName} reacted on your post: {reaction.ReactionType}"
            };

            var payload = JsonSerializer.Serialize(@event);

            await _eventBus.PublishAsync(
                    "notification.created",
                    payload
            );

            return _mapper.Map<ReactionDto>(reaction);
        }
    }
}
