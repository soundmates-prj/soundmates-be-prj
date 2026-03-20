using AccountContentService.Application.DTOs;
using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using AccountContentService.Domain.Entities;
using AccountContentService.Domain.Enums;
using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogComments.Commands.CreateComment
{
    public class CreateCommentHandler
     : IRequestHandler<CreateCommentCommand, CommentDto>
    {
        private readonly ICommentRepository _repository;
        private readonly IBlogPostRepository _postRepository;
        private readonly IMapper _mapper;   
        private readonly IUserServiceClient _userClient;

        public CreateCommentHandler(
            ICommentRepository repository,
            IBlogPostRepository postRepository,
            IMapper mapper,
            IUserServiceClient userClient)
        {
            _repository = repository;
            _postRepository = postRepository;
            _mapper = mapper;
            _userClient = userClient;
        }

        public async Task<CommentDto> Handle(
            CreateCommentCommand request,
            CancellationToken cancellationToken)
        {
            var post = await _postRepository.GetByIdAsync(request.PostId, cancellationToken);

            if (post == null)
            {
                throw new NotFoundException("Post not found");
            }

            var comment = _mapper.Map<BlogComment>(request);
            comment.Status = CommentStatus.Active.ToString();
            comment.CreatedAt  = DateTime.UtcNow;

            await _repository.AddAsync(comment);

            var userProfile = await _userClient.GetMyProfile();
            var respose = _mapper.Map<CommentDto>(comment);
            respose.userProfile = userProfile;

            return respose;
        }
    }
}
