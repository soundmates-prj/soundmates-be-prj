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
        private readonly IUserServiceClient _userServiceClient;

        public CreateCommentHandler(
            ICommentRepository repository,
            IBlogPostRepository postRepository,
            IMapper mapper,
            IUserServiceClient userServiceClient)
        {
            _repository = repository;
            _postRepository = postRepository;
            _mapper = mapper;
            _userServiceClient = userServiceClient;
        }

        public async Task<CommentDto> Handle(
            CreateCommentCommand request,
            CancellationToken cancellationToken)
        {
            var user = await _userServiceClient.GetMyProfile();
            if (user == null)
            {
                throw new Exception("User is not available!");
            }
            var post = await _postRepository.GetByIdAsync(request.PostId, cancellationToken);

            if (post == null)
            {
                throw new NotFoundException("Post not found");
            }

            var comment = _mapper.Map<BlogComment>(request);
            comment.Status = CommentStatus.Active.ToString();
            comment.CreatedAt  = DateTime.UtcNow;
            comment.UserAvatarUrl = user.ProfileImageUrl ?? string.Empty;
            comment.UserFullName = $"{user.FirstName ?? ""} {user.LastName ?? ""}".Trim() ?? string.Empty;

            await _repository.AddAsync(comment);
            var respose = _mapper.Map<CommentDto>(comment);

            return respose;
        }
    }
}
