using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Features.BlogPosts.Queries.GetPopularPosts
{
    public class PopularPostsResponse
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string ContentText { get; set; } = string.Empty;

        public string? AudioUrl { get; set; }
        public string? ImgUrl { get; set; }

        public string? PrivacyScope { get; set; }

        public string? MoodTag { get; set; }

        public string Status { get; set; } = string.Empty;

        public bool IsGenerated { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }

        public int ReactionCount { get; set; }

        public int CommentCount { get; set; }
    }
}
