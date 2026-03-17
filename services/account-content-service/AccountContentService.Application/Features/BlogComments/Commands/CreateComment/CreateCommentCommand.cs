using AccountContentService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogComments.Commands.CreateComment
{
    public class CreateCommentCommand : IRequest<CommentDto>
    {
        public Guid PostId { get; set; }
        public Guid? ParentCommentId { get; set; }
        public Guid UserId { get; set; }
        public string Content { get; set; }

    }
}
