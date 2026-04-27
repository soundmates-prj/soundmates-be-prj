using System;
using System.Text.Json.Serialization;

namespace Shared.Contracts.Events.Config;

/// <summary>
/// Published when Gemini AI configuration is updated.
/// Routing key: config.gemini.updated
/// </summary>
public sealed class GeminiConfigUpdatedEvent : BaseIntegrationEvent
{
    [JsonPropertyName("provider")]
    public string Provider { get; init; } = "Gemini";

    [JsonPropertyName("apiKey")]
    public string ApiKey { get; init; } = string.Empty;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// When true, consumers should clear/reset the Gemini configuration.
    /// </summary>
    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; init; }
}
