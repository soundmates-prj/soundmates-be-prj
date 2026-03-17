using AccountContentService.Application.DTOs;
using AccountContentService.Application.Exceptions;
using AccountContentService.Application.Features.BlogComments.Commands.DeleteComment;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogComments.Commands.ReportComment
{
    public class ReportCommentHandler : IRequestHandler<ReportCommentCommand, bool>
    {
        private readonly ICommentRepository _repository;

        public ReportCommentHandler(ICommentRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(
            ReportCommentCommand request,
            CancellationToken cancellationToken)
        {
            var comment = await _repository.GetByIdAsync(request.CommentId, cancellationToken);

            if (comment == null)
            {
                throw new NotFoundException("Comment not found");
            }
            comment.Status = CommentStatus.Banned.ToString();
            await _repository.UpdateAsync(comment);

            return true;
        }
    }
}
