using AccountContentService.Domain.Entities;

namespace AccountContentService.Application.DTOs;

public class ReportedPostDto
{
    public PostDto postDto { get; set; } = new PostDto();
    public int ReportCount { get; set; }
    public DateTime CreatedAt { get; set; }
}