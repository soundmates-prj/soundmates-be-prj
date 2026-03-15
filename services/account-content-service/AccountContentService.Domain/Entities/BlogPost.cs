namespace AccountContentService.Domain.Entities;

public class BlogPost
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string ContentText { get; set; } = string.Empty;

    public string? AudioUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public string? PrivacyScope { get; set; } = "Public";

    public string? MoodTag { get; set; }

    public string Status { get; set; } = "Draft";

    public bool IsGenerated { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime? PublishedAt { get; set; }

    public ICollection<BlogComment> Comments { get; set; } = new List<BlogComment>();

    public ICollection<PostReaction> Reactions { get; set; } = new List<PostReaction>();
}