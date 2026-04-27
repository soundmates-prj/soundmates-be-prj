using AccountContentService.Domain.Enums;

namespace AccountContentService.Api.Contracts.Requests
{
    public class CreatePostRequest
    {
        public required string Title { get; set; }

        public required string ContentText { get; set; }

        public string? AudioUrl { get; set; }
        public string? ImageUrl { get; set; }

        public string PrivacyScope { get; set; } = "Public";

        public string? MoodTag { get; set; } = string.Empty;
    }

    public class UpdatePostRequest
    {
        public string? Title { get; set; }

        public string? ContentText { get; set; }

        public string? AudioUrl { get; set; }
        public string? ImageUrl { get; set; }

        public string? PrivacyScope { get; set; }

        public string? MoodTag { get; set; }
    }

    public class GetPostsRequest
    {
        public int Page { get; set; }

        public int PageSize { get; set; }

        public PostStatus? Status { get; set; }

        public string? MoodTag { get; set; }
    }

    public class ShareMusicPostRequest
    {
        public required string TrackId { get; set; }
        public required string Title { get; set; }
        public required string Artist { get; set; }
        public required string AlbumImage { get; set; }
        public string? PreviewUrl { get; set; }
        public required string Template { get; set; }
    }
}
