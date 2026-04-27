namespace AccountContentService.Api.Contracts.Requests;

public sealed class GeminiConfigRequest
{
    public string Provider { get; set; } = "Gemini";

    public string ApiKey { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
