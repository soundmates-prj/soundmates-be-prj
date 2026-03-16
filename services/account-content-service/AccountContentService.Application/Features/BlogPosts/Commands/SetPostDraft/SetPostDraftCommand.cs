using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPosts.Commands.SetPostDraft
{
    public class SetPostDraftCommand : IRequest<bool>
    {
        public Guid PostId { get; set; }

        public SetPostDraftCommand(Guid postId)
        {
            PostId = postId;
        }
    }
}
