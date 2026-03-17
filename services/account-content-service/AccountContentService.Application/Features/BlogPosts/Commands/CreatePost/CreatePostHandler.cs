using AccountContentService.Application.DTOs;
using AccountContentService.Application.Interfaces.Repositories;
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
        private readonly IMapper _mapper;

        public CreatePostHandler(
            IBlogPostRepository postRepository,
            IMapper mapper)
        {
            _postRepository = postRepository;
            _mapper = mapper;
        }

        public async Task<PostDto> Handle(
            CreatePostCommand request,
            CancellationToken cancellationToken)
        {
            var post = _mapper.Map<BlogPost>(request);
            post.PublishedAt = DateTime.UtcNow;

            await _postRepository.AddAsync(post);

            return _mapper.Map<PostDto>(post);
        }
    }
}
