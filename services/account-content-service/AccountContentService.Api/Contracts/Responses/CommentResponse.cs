using AccountContentService.Application.DTOs;

namespace AccountContentService.Api.Contracts.Responses
{
    public class CommentResponse
    {
        public Guid Id { get; set; }

        public Guid PostId { get; set; }
        public Guid ParentCommentId { get; set; }

        public Guid UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserAvatarUrl { get; set; } = string.Empty;

        public string Content { get; set; }

        public string Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
        public List<CommentResponse> Replies { get; set; } = new();
    }
}
