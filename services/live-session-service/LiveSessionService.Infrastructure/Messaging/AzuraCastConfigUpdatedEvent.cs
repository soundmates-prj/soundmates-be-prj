using System.Text.Json.Serialization;

namespace LiveSessionService.Infrastructure.Messaging;

internal sealed class AzuraCastConfigUpdatedEvent
{
    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; init; } = string.Empty;

    [JsonPropertyName("apiKey")]
    public string ApiKey { get; init; } = string.Empty;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; init; }

    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; init; }
}
