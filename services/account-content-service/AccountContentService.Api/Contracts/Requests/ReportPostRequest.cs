namespace AccountContentService.Api.Contracts.Requests;

public class ReportPostRequest
{
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
}