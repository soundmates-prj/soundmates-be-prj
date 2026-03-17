using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPosts.Commands.SetPostArchived
{
    public class SetPostArchivedCommand : IRequest<bool>
    {
        public Guid PostId { get; set; }

        public SetPostArchivedCommand(Guid postId)
        {
            PostId = postId;
        }
    }
}
