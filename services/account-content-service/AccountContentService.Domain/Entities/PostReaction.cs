namespace AccountContentService.Domain.Entities;

public class PostReaction
{
    public Guid Id { get; set; }

    public Guid PostId { get; set; }

    public Guid UserId { get; set; }

    public string ReactionType { get; set; } = "like";

    public DateTime CreatedAt { get; set; }

    public BlogPost Post { get; set; } = null!;
}