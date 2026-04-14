using AccountContentService.Application.Abstractions;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using AccountContentService.Domain.Entities;
using AccountContentService.Domain.Enums;
using AutoMapper;
using MediatR;
using shared.Contracts.Events.Notifications;
using System.Text.Json;

namespace AccountContentService.Application.Features.BlogComments.Commands.CreateComment
{
    public class CreateCommentHandler : IRequestHandler<CreateCommentCommand, CommentDto>
    {
        private readonly ICommentRepository _repository;
        private readonly IBlogPostRepository _postRepository;
        private readonly IMapper _mapper;
        private readonly IUserProfileCache _userProfileCache;
        private readonly IMessageBusPublisher _eventBus;

        public CreateCommentHandler(
            ICommentRepository repository,
            IBlogPostRepository postRepository,
            IMapper mapper,
            IUserProfileCache userProfileCache,
            IMessageBusPublisher eventBus)
        {
            _repository = repository;
            _postRepository = postRepository;
            _mapper = mapper;
            _userProfileCache = userProfileCache;
            _eventBus = eventBus;
        }

        public async Task<CommentDto> Handle(
            CreateCommentCommand request,
            CancellationToken cancellationToken)
        {
            var userProfile = await _userProfileCache.GetProfileAsync(request.UserId, cancellationToken);

            var post = await _postRepository.GetByIdAsync(request.PostId, cancellationToken);

            if (post == null)
            {
                throw new NotFoundException("Post not found");
            }

            var comment = _mapper.Map<BlogComment>(request);
            comment.Status = CommentStatus.Active.ToString();
            comment.CreatedAt = DateTime.UtcNow;
            comment.UserAvatarUrl = userProfile.AvatarUrl ?? string.Empty;
            comment.UserFullName = userProfile.FullName;

            await _repository.AddAsync(comment);

            if (comment.UserId != post.UserId)
            {
                var @event = new NotificationEvent
                {
                    Title = "New Comment",
                    SendUserId = comment.UserId,
                    ReceiveUserId = post.UserId,
                    ReferenceId = post.Id,
                    Type = "post-comment",
                    Message = $"{userProfile.FullName} commented on your post: {comment.Content}"
                };

                var payload = JsonSerializer.Serialize(@event);

                await _eventBus.PublishAsync(
                        "notification.created",
                        payload
                );
            }

            return _mapper.Map<CommentDto>(comment);
        }
    }
}
