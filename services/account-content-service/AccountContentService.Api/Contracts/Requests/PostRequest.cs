using AccountContentService.Domain.Enums;

namespace AccountContentService.Api.Contracts.Requests
{
    public class CreatePostRequest
    {
        public string Title { get; set; } = string.Empty;

        public string ContentText { get; set; } = string.Empty;

        public string? AudioUrl { get; set; }

        public string? PrivacyScope { get; set; }

        public string? MoodTag { get; set; }
    }

    public class UpdatePostRequest
    {
        public string Title { get; set; }

        public string ContentText { get; set; }

        public string? AudioUrl { get; set; }

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
}
