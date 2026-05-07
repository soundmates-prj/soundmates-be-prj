namespace AccountContentService.Application.DTOs;

public class BlogReportDto
{
    public Guid Id { get; set; }
    public Guid BlogPostId { get; set; }
    public Guid ReporterUserId { get; set; }
    public string ReporterUserName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}