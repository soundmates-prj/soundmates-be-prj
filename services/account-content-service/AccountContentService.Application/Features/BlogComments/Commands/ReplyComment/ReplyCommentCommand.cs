using AccountContentService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogComments.Commands.ReplyComment
{
    public class ReplyCommentCommand : IRequest<CommentDto>
    {
        public Guid ParentCommentId { get; set; }

        public Guid UserId { get; set; }

        public string Content { get; set; }

    }
}
