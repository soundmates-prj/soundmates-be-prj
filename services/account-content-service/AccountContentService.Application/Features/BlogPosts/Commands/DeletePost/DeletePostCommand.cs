using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPosts.Commands.DeletePost
{
    public class DeletePostCommand : IRequest<bool>
    {
        public Guid PostId { get; set; }

        public DeletePostCommand(Guid postId)
        {
            PostId = postId;
        }
    }
}
