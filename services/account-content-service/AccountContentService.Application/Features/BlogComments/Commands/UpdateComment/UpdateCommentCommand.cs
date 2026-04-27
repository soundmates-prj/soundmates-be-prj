using AccountContentService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogComments.Commands.UpdateComment
{
    public class UpdateCommentCommand : IRequest<CommentDto>
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
