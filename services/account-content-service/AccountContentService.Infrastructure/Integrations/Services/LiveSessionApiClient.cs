using AccountContentService.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AccountContentService.Infrastructure.Integrations.Services;

public class LiveSessionApiClient : ILiveSessionApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LiveSessionApiClient> _logger;

    public LiveSessionApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<LiveSessionApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        var baseUrl = configuration["LiveSessionService:BaseUrl"] ?? "http://localhost:5004";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<PodcastDto?> GetPodcastAsync(Guid podcastId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/podcast/{podcastId}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var apiResult = await response.Content.ReadFromJsonAsync<ApiResult<PodcastDto>>(cancellationToken: cancellationToken);
                return apiResult?.Data;
            }
            
            _logger.LogWarning("Failed to fetch podcast {PodcastId} from live-session-service. Status: {StatusCode}", podcastId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching podcast {PodcastId} from live-session-service", podcastId);
            return null;
        }
    }

    public async Task<bool> GrantPodcastAccessAsync(Guid podcastId, Guid userId, decimal price, CancellationToken cancellationToken)
    {
        try
        {
            var request = new { UserId = userId, Price = price };
            var response = await _httpClient.PostAsJsonAsync($"/api/v1/podcast/{podcastId}/grant-access", request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            _logger.LogWarning("Failed to grant podcast {PodcastId} access to user {UserId}. Status: {StatusCode}", podcastId, userId, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error granting podcast {PodcastId} access to user {UserId}", podcastId, userId);
            return false;
        }
    }

    private class ApiResult<T>
    {
        public T? Data { get; set; }
    }
}
