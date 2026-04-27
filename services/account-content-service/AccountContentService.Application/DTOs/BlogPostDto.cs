using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.DTOs
{
    public class PostDto
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string ContentText { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public string UserAvatarUrl { get; set; } = string.Empty;

        public string? AudioUrl { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;

        public string? PrivacyScope { get; set; } = "Public";

        public string? MoodTag { get; set; }

        public string Status { get; set; } = "Draft";

        public bool IsGenerated { get; set; } = false;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }

        public string? PostType { get; set; }
        public ShareMusicDto? ShareMusic { get; set; }

        //public List<CommentDto> Comments { get; set; } = new();
        //public List<ReactionDto> Reactions { get; set; } = new();

    }

    public class ShareMusicDto
    {
        public string TrackId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string AlbumImage { get; set; } = string.Empty;
        public string? PreviewUrl { get; set; }
        public string Template { get; set; } = "gradient";
    }
}
