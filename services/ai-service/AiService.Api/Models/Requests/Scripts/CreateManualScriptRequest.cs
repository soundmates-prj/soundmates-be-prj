namespace AiService.Api.Models.Requests.Scripts;

public class CreateManualScriptRequest
{
    public required string ContentText { get; set; }
    public string? Title { get; set; }
    public string? Topic { get; set; }
}
