using System;
using System.Text.Json.Serialization;

namespace Shared.Contracts.Events.Config;

/// <summary>
/// Published when AzuraCast configuration is updated.
/// Routing key: config.azuracast.updated
/// </summary>
public sealed class AzuraCastConfigUpdatedEvent : BaseIntegrationEvent
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
    /// When true, consumers should clear/reset the AzuraCast configuration.
    /// </summary>
    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; init; }
}
