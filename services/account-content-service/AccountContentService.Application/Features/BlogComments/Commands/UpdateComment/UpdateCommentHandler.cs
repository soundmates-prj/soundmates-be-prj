using AccountContentService.Application.DTOs;
using AccountContentService.Application.Exceptions;

using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using AccountContentService.Domain.Enums;
using AutoMapper;
using MediatR;
using System.Diagnostics;

namespace AccountContentService.Application.Features.BlogComments.Commands.UpdateComment
{
    public class UpdateCommentHandler : IRequestHandler<UpdateCommentCommand, CommentDto>
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IMapper _mapper;
        private readonly IUserServiceClient _userClient;

        public UpdateCommentHandler(
            ICommentRepository commentRepository,
            IMapper mapper,
            IUserServiceClient userClient)
        {
            _commentRepository = commentRepository;
            _mapper = mapper;
            _userClient = userClient;
        }

        public async Task<CommentDto> Handle(
            UpdateCommentCommand request,
            CancellationToken cancellationToken)
        {
            var comment = await _commentRepository.GetByIdAsync(request.Id, cancellationToken);

            if (comment == null)
            {
                throw new NotFoundException("Comment not found");
            }

            _mapper.Map(request, comment);

            comment.Status = CommentStatus.Edited.ToString();
            comment.UpdatedAt = DateTime.UtcNow;

            await _commentRepository.UpdateAsync(comment);
            var response = _mapper .Map<CommentDto>(comment);
            var userProfile = await _userClient.GetMyProfile();
            response.userProfile = userProfile;

            return _mapper.Map<CommentDto>(response);
        }
    }
}
