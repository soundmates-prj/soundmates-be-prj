namespace AccountContentService.Api.Contracts.Responses
{
    public class PostResponse
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserAvatarUrl { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string ContentText { get; set; } = string.Empty;

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
        public ShareMusicResponse? ShareMusic { get; set; }

        //public List<CommentResponse> Comments { get; set; } = new();
        //public List<ReactionResponse> Reactions { get; set; } = new();
    }

    public class ShareMusicResponse
    {
        public string TrackId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string AlbumImage { get; set; } = string.Empty;
        public string? PreviewUrl { get; set; }
        public string Template { get; set; } = "gradient";
    }
}
