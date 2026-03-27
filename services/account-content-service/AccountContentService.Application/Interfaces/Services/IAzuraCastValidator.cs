namespace AccountContentService.Application.Interfaces.Services;

/// <summary>
/// Validates AzuraCast credentials by calling the AzuraCast health endpoint.
/// </summary>
public interface IAzuraCastValidator
{
    Task ValidateAsync(string baseUrl, string apiKey, CancellationToken cancellationToken);
}
