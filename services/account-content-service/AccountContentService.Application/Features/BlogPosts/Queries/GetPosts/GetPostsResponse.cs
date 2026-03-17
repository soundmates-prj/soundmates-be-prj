using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPosts.Queries.GetPosts
{
    public class GetPostsResponse
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string ContentText { get; set; } = string.Empty;

        public string MoodTag { get; set; }  = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
