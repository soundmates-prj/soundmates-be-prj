namespace AiService.Infrastructure.Clients;

public class TtsOptions
{
    public string? Provider { get; set; }
    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public string? Model { get; set; }
    public string? SynthesizePath { get; set; }
    public string? AudioFormat { get; set; }
    public string? PromptTemplate { get; set; }
    public int TimeoutSeconds { get; set; } = 100;
}

