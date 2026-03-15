using AccountContentService.Application.Abstractions;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Features.BlogPosts.Commands.CreatePost;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using AccountContentService.Domain.Enums;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AccountContentService.Application.Features.BlogPosts.Commands.UpdatePost
{
    internal class UpdatePostHandler : IRequestHandler<UpdatePostCommand, PostDto>
    {
        private readonly IBlogPostRepository _postRepository;
        private readonly IMapper _mapper;

        public UpdatePostHandler(
            IBlogPostRepository postRepository,
            IMapper mapper)
        {
            _postRepository = postRepository;
            _mapper = mapper;
        }

        public async Task<PostDto> Handle(
            UpdatePostCommand request,
            CancellationToken cancellationToken)
        {
            var post = await _postRepository.GetByIdAsync(request.PostId, cancellationToken);

            if (post == null)
            {
                throw new NotFoundException("Post not found");
            }

            
            _mapper.Map(request, post);
            if (post.Status != PostStatus.Draft.ToString())
            {
                post.Status = PostStatus.Edited.ToString();
            }
            post.UpdatedAt = DateTime.UtcNow;
            Debug.WriteLine(post);

            await _postRepository.UpdateAsync(post);

            return _mapper.Map<PostDto>(post);
        }
    }
}
