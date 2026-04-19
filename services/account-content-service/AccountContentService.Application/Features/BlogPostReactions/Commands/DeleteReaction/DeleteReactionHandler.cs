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
        private readonly AccountContentService.Application.Interfaces.Repositories.INotificationRepository _notificationRepository;
        private readonly AccountContentService.Application.Interfaces.Services.IUserProfileCache _userProfileCache;
        private readonly AccountContentService.Application.Interfaces.INotificationPusher _notificationPusher;
        private readonly AccountContentService.Application.Interfaces.Repositories.IBlogPostRepository _postRepository;
        
        public DeleteReactionHandler(
            IPostReactionRepository repository,
            AccountContentService.Application.Interfaces.Repositories.INotificationRepository notificationRepository,
            AccountContentService.Application.Interfaces.Services.IUserProfileCache userProfileCache,
            AccountContentService.Application.Interfaces.INotificationPusher notificationPusher,
            AccountContentService.Application.Interfaces.Repositories.IBlogPostRepository postRepository)
        {
            _repository = repository;
            _notificationRepository = notificationRepository;
            _userProfileCache = userProfileCache;
            _notificationPusher = notificationPusher;
            _postRepository = postRepository;
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

            var post = await _postRepository.GetByIdAsync(request.PostId, cancellationToken);
            await _repository.DeleteAsync(reaction);

            var userProfile = await _userProfileCache.GetProfileAsync(request.UserId, cancellationToken);
            if (userProfile != null && !string.IsNullOrEmpty(userProfile.FullName) && post != null)
            {
                await _notificationRepository.DeleteByReferenceAndTypeAsync(
                    referenceId: reaction.PostId,
                    type: "post-reaction",
                    messageKeyword: userProfile.FullName,
                    cancellationToken: cancellationToken);

                await _notificationPusher.PushDeleteToUserAsync(
                    userId: post.UserId, 
                    referenceId: reaction.PostId, 
                    type: "post-reaction", 
                    cancellationToken: cancellationToken);
            }

            return true;
        }
    }
}
