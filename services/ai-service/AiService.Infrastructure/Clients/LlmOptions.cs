namespace AiService.Infrastructure.Clients;

public class LlmOptions
{
    public string? Provider { get; set; }
    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public string? Model { get; set; }
}

