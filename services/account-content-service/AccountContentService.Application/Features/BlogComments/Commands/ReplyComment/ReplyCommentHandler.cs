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
using System.Xml.Linq;

namespace AccountContentService.Application.Features.BlogComments.Commands.ReplyComment
{
    public class ReplyCommentHandler : IRequestHandler<ReplyCommentCommand, CommentDto>
    {
        private readonly ICommentRepository _repository;
        private readonly IMapper _mapper;
        private readonly IUserProfileCache _userProfileCache;
        private readonly IMessageBusPublisher _eventBus;

        public ReplyCommentHandler(
            ICommentRepository repository,
            IMapper mapper,
            IUserProfileCache userProfileCache,
            IMessageBusPublisher eventBus)
        {
            _repository = repository;
            _mapper = mapper;
            _userProfileCache = userProfileCache;
            _eventBus = eventBus;
        }

        public async Task<CommentDto> Handle(
            ReplyCommentCommand request,
            CancellationToken cancellationToken)
        {
            var userProfile = await _userProfileCache.GetProfileAsync(request.UserId, cancellationToken);

            var parent = await _repository.GetByIdAsync(request.ParentCommentId, cancellationToken)
                ?? throw new NotFoundException("Comment not found");

            var reply = _mapper.Map<BlogComment>(request);
            reply.Status = CommentStatus.Active.ToString();
            reply.CreatedAt = DateTime.UtcNow;
            reply.PostId = parent.PostId;
            reply.UserAvatarUrl = userProfile.AvatarUrl ?? string.Empty;
            reply.UserFullName = userProfile.FullName;

            await _repository.AddAsync(reply);


            var @event = new NotificationEvent
            {
                Title = "Reply Comment",
                SendUserId = reply.UserId,
                ReceiveUserId = parent.UserId,
                ReferenceId = parent.Id,
                Type = "comment-reply",
                Message = $"{userProfile.FullName} replied on your comment: {reply.Content}"
            };

            var payload = JsonSerializer.Serialize(@event);

            await _eventBus.PublishAsync(
                    "notification.created",
                    payload
            );

            return _mapper.Map<CommentDto>(reply);
        }
    }
}
