using System.Text.Json.Serialization;

namespace Shared.Contracts.Events;

public sealed class GeminiConfigUpdatedEvent
{
    [JsonPropertyName("provider")]
    public string Provider { get; init; } = "Gemini";

    [JsonPropertyName("apiKey")]
    public string ApiKey { get; init; } = string.Empty;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; init; }

    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; init; }
}
