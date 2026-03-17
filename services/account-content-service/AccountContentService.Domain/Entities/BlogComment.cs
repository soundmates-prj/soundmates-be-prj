namespace AccountContentService.Domain.Entities;

public class BlogComment
{
    public Guid Id { get; set; }

    public Guid PostId { get; set; }

    public Guid UserId { get; set; }
    public Guid ParentCommentId { get; set; }

    public string Content { get; set; } = string.Empty;

    public string Status { get; set; } = "active";

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public BlogPost Post { get; set; } = null!;
}