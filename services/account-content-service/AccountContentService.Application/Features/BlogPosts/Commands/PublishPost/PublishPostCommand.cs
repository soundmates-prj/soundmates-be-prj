using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPosts.Commands.PublishPost
{
    public class PublishPostCommand : IRequest<bool>
    {
        public Guid PostId { get; set; }

        public PublishPostCommand(Guid postId)
        {
            PostId = postId;
        }
    }
}
