namespace AccountContentService.Api.Contracts.Requests;

public sealed class AzuraCastConfigRequest
{
    /// <summary>
    /// Base URL of the AzuraCast instance (e.g., http://azuracast.local:5000).
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// AzuraCast API key (will be encrypted before storage).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Whether this configuration should be active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
