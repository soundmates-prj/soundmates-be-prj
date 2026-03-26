using AccountContentService.Application.DTOs;
using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using AccountContentService.Domain.Entities;
using AccountContentService.Domain.Enums;
using AutoMapper;
using MediatR;

namespace AccountContentService.Application.Features.BlogPosts.Commands.CreatePost
{
    public class CreatePostHandler : IRequestHandler<CreatePostCommand, PostDto>
    {
        private readonly IBlogPostRepository _postRepository;
        private readonly IUserProfileCache _userProfileCache;
        private readonly IMapper _mapper;

        public CreatePostHandler(
            IBlogPostRepository postRepository,
            IUserProfileCache userProfileCache,
            IMapper mapper)
        {
            _postRepository = postRepository;
            _userProfileCache = userProfileCache;
            _mapper = mapper;
        }

        public async Task<PostDto> Handle(
            CreatePostCommand request,
            CancellationToken cancellationToken)
        {
            var userProfile = await _userProfileCache.GetProfileAsync(request.UserId, cancellationToken);

            var post = _mapper.Map<BlogPost>(request);
            post.PublishedAt = DateTime.UtcNow;
            post.CreatedAt = DateTime.UtcNow;
            post.UserFullName = userProfile.FullName;
            post.UserAvatarUrl = userProfile.AvatarUrl ?? string.Empty;

            await _postRepository.AddAsync(post);

            return _mapper.Map<PostDto>(post);
        }
    }
}
