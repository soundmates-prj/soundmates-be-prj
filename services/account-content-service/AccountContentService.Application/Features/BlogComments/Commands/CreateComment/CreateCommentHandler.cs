using AccountContentService.Application.DTOs;
using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using AccountContentService.Domain.Entities;
using AccountContentService.Domain.Enums;
using AutoMapper;
using MediatR;

namespace AccountContentService.Application.Features.BlogComments.Commands.CreateComment
{
    public class CreateCommentHandler : IRequestHandler<CreateCommentCommand, CommentDto>
    {
        private readonly ICommentRepository _repository;
        private readonly IBlogPostRepository _postRepository;
        private readonly IMapper _mapper;
        private readonly IUserProfileCache _userProfileCache;

        public CreateCommentHandler(
            ICommentRepository repository,
            IBlogPostRepository postRepository,
            IMapper mapper,
            IUserProfileCache userProfileCache)
        {
            _repository = repository;
            _postRepository = postRepository;
            _mapper = mapper;
            _userProfileCache = userProfileCache;
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
            return _mapper.Map<CommentDto>(comment);
        }
    }
}
