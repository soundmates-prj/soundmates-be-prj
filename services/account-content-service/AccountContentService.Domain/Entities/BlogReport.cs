namespace AccountContentService.Domain.Entities;

public class BlogReport
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BlogPostId { get; set; }
    public BlogPost BlogPost { get; set; } = null!;

    public Guid ReporterUserId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}