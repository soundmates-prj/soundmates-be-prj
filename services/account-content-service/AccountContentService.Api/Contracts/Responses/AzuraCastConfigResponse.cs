namespace AccountContentService.Api.Contracts.Responses;

public sealed class AzuraCastConfigResponse
{
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Masked API key showing only last 4 characters (e.g., ****xxxx).
    /// </summary>
    public string MaskedApiKey { get; set; } = string.Empty;

    public bool IsConfigured { get; set; }

    public bool IsActive { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
