using AccountContentService.Application.DTOs;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using AccountContentService.Domain.Entities;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace AccountContentService.Application.Features.BlogPosts.Commands.CreatePost
{
    public class CreatePostHandler : IRequestHandler<CreatePostCommand, PostDto>
    {
        private readonly IBlogPostRepository _postRepository;
        private readonly IUserServiceClient _userServiceClient;
        private readonly IMapper _mapper;

        public CreatePostHandler(
            IBlogPostRepository postRepository,
            IUserServiceClient userServiceClient,
            IMapper mapper)
        {
            _postRepository = postRepository;
            _userServiceClient = userServiceClient;
            _mapper = mapper;
        }

        public async Task<PostDto> Handle(
            CreatePostCommand request,
            CancellationToken cancellationToken)
        {
            var user = await _userServiceClient.GetMyProfile();
            if (user == null)
            {
                throw new Exception("User is not available!");
            }
            var post = _mapper.Map<BlogPost>(request);
            post.PublishedAt = DateTime.UtcNow;
            post.CreatedAt = DateTime.UtcNow;
            post.UserFullName = $"{user.FirstName ?? ""} {user.LastName ?? ""}".Trim() ?? string.Empty;
            post.UserAvatarUrl = user.ProfileImageUrl ?? string.Empty;


            await _postRepository.AddAsync(post);

            return _mapper.Map<PostDto>(post);
        }
    }
}
