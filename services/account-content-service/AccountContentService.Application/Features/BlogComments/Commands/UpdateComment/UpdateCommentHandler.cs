using AccountContentService.Application.DTOs;
using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Enums;
using AutoMapper;
using MediatR;

namespace AccountContentService.Application.Features.BlogComments.Commands.UpdateComment
{
    public class UpdateCommentHandler : IRequestHandler<UpdateCommentCommand, CommentDto>
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IMapper _mapper;

        public UpdateCommentHandler(
            ICommentRepository commentRepository,
            IMapper mapper)
        {
            _commentRepository = commentRepository;
            _mapper = mapper;
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

            return _mapper.Map<CommentDto>(comment);
        }
    }
}
