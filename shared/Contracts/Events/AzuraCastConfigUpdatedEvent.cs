using System.Text.Json.Serialization;

namespace Shared.Contracts.Events;

public sealed class AzuraCastConfigUpdatedEvent
{
    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; init; } = string.Empty;

    [JsonPropertyName("apiKey")]
    public string ApiKey { get; init; } = string.Empty;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// When true, the consumer should clear/reset the AzuraCast configuration.
    /// </summary>
    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; init; }
}
