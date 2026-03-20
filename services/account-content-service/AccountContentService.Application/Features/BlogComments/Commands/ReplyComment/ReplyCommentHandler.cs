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

namespace AccountContentService.Application.Features.BlogComments.Commands.ReplyComment
{
    public class ReplyCommentHandler
    : IRequestHandler<ReplyCommentCommand, CommentDto>
    {
        private readonly ICommentRepository _repository;
        private readonly IMapper _mapper;
        private readonly IUserServiceClient _userClient;

        public ReplyCommentHandler(ICommentRepository repository, IMapper mapper, IUserServiceClient userClient)
        {
            _repository = repository;
            _mapper = mapper;
            _userClient = userClient;
        }

        public async Task<CommentDto> Handle(
            ReplyCommentCommand request,
            CancellationToken cancellationToken)
        {
            var parent = await _repository.GetByIdAsync(request.ParentCommentId, cancellationToken);

            if (parent == null)
            {
                throw new NotFoundException("Comment not found");
            }

            var reply = _mapper.Map<BlogComment>(request);
            reply.Status = CommentStatus.Active.ToString();
            reply.CreatedAt = DateTime.UtcNow;
            reply.PostId = parent.PostId;

            await _repository.AddAsync(reply);

            var userProfile = await _userClient.GetMyProfile();
            var response = _mapper.Map<CommentDto>(reply);
            response.userProfile = userProfile;

            return response;
        }
    }
}
