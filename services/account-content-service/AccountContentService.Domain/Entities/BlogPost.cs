namespace AccountContentService.Domain.Entities;

public class BlogPost
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string ContentText { get; set; } = string.Empty;

    public string? AudioUrl { get; set; }

    public bool IsActive { get; set; }

    public string? PrivacyScope { get; set; }

    public string? MoodTag { get; set; }

    public bool IsGenerated { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime? PublishedAt { get; set; }

    public ICollection<BlogComment> Comments { get; set; } = new List<BlogComment>();

    public ICollection<PostReaction> Reactions { get; set; } = new List<PostReaction>();
}