using AccountContentService.Application.DTOs;
using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using AccountContentService.Domain.Entities;
using AccountContentService.Domain.Enums;
using AutoMapper;
using MediatR;

namespace AccountContentService.Application.Features.BlogComments.Commands.ReplyComment
{
    public class ReplyCommentHandler : IRequestHandler<ReplyCommentCommand, CommentDto>
    {
        private readonly ICommentRepository _repository;
        private readonly IMapper _mapper;
        private readonly IUserProfileCache _userProfileCache;

        public ReplyCommentHandler(
            ICommentRepository repository,
            IMapper mapper,
            IUserProfileCache userProfileCache)
        {
            _repository = repository;
            _mapper = mapper;
            _userProfileCache = userProfileCache;
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

            return _mapper.Map<CommentDto>(reply);
        }
    }
}
