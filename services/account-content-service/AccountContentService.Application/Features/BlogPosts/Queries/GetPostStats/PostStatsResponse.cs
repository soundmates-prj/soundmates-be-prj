using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPosts.Queries.GetPostStats
{
    public class PostStatsResponse
    {
        public Guid PostId { get; set; }

        public int ReactionCount { get; set; }

        public int CommentCount { get; set; }

        public int ViewCount { get; set; }

        public DateTime? PublishedAt { get; set; }
    }
}
