namespace AccountContentService.Domain.Entities;

public class ContentReport
{
    public Guid Id { get; set; }

    public string ContentType { get; set; } = string.Empty;

    public Guid ContentId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string Status { get; set; } = "pending";

    public Guid? ReviewedBy { get; set; }

    public DateTime CreatedAt { get; set; }
}   