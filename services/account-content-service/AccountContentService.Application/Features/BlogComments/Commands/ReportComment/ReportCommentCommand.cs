using AccountContentService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogComments.Commands.ReportComment
{
    public class ReportCommentCommand : IRequest<bool>
    {
        public Guid CommentId { get; set; }

        public ReportCommentCommand(Guid commentId)
        {
            CommentId = commentId;
        }
    }
}
