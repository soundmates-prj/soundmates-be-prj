using AccountContentService.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AccountContentService.Infrastructure.Integrations.Services;

public sealed class AzuraCastValidator : IAzuraCastValidator
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzuraCastValidator> _logger;

    public AzuraCastValidator(HttpClient httpClient, ILogger<AzuraCastValidator> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task ValidateAsync(string baseUrl, string apiKey, CancellationToken cancellationToken)
    {
        try
        {
            _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);

            var response = await _httpClient.GetAsync("/api", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var statusCode = (int)response.StatusCode;
                if (statusCode == 403)
                    throw new UnauthorizedAccessException(
                        "AzuraCast API key is invalid or lacks required permissions (403 Forbidden). " +
                        "Ensure the API key has 'Manage Station' permissions.");
                throw new InvalidOperationException($"AzuraCast health check failed with status {statusCode}.");
            }

            _logger.LogDebug("AzuraCast API key validated for {BaseUrl}", baseUrl);
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                $"Cannot connect to AzuraCast at {baseUrl}. Verify the Base URL and that AzuraCast is accessible.", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            throw new InvalidOperationException(
                $"Connection to AzuraCast at {baseUrl} timed out (10s). Verify the URL is correct.", ex);
        }
    }
}
