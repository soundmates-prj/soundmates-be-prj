using AiService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace AiService.Infrastructure.Clients;

public class PodcastSyncClient : IPodcastSyncClient
{
    private readonly HttpClient _http;
    private readonly string _liveSessionUrl;
    private readonly ILogger<PodcastSyncClient> _logger;

    public PodcastSyncClient(HttpClient http, IConfiguration configuration, ILogger<PodcastSyncClient> logger)
    {
        _http = http;
        _logger = logger;
        
        // Load from configuration
        var url = configuration["ExternalServices:LiveSessionService"] 
                  ?? configuration["LIVE_SESSION_URL"] 
                  ?? "http://live-session-service:8080";
        
        _liveSessionUrl = url.TrimEnd('/');
    }

    public async Task<PodcastSyncResponse> SyncToLiveServiceAsync(PodcastSyncRequest request, CancellationToken ct)
    {
        var endpoint = $"{_liveSessionUrl}/api/v1/podcast";
        
        _logger.LogInformation("Syncing podcast to {Endpoint}: {Title}", endpoint, request.Title);
        
        try
        {
            // First create the Podcast meta
            var podcastPayload = new
            {
                createdBy = request.CreatedBy,
                title = request.Title,
                description = request.Description,
                author = request.Author ?? "AI Generated",
                type = request.Type ?? "story",
                banner = request.Banner
            };

            using var podcastResp = await _http.PostAsJsonAsync(endpoint, podcastPayload, ct);
            
            if (!podcastResp.IsSuccessStatusCode)
            {
                var error = await podcastResp.Content.ReadAsStringAsync(ct);
                _logger.LogError("Failed to create podcast on live service: {Error}", error);
                return new PodcastSyncResponse(false, null, error);
            }

            var result = await podcastResp.Content.ReadFromJsonAsync<PodcastCreateResponse>(cancellationToken: ct);
            var podcastId = result?.Data?.Id;

            if (podcastId == null)
            {
                _logger.LogWarning("Podcast created but ID is null in response.");
                return new PodcastSyncResponse(false, null, "Failed to retrieve podcast ID");
            }

            _logger.LogInformation("Podcast created on live service: {Id}. Now syncing episode.", podcastId);

            // In a real scenario, you'd also create an episode for this podcast.
            // Since there is no episode API yet, this client completes the podcast header sync.
            
            return new PodcastSyncResponse(true, podcastId.ToString(), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during podcast sync to live service");
            return new PodcastSyncResponse(false, null, ex.Message);
        }
    }

    private class PodcastCreateResponse
    {
        public bool Success { get; set; }
        public PodcastData? Data { get; set; }
    }

    private class PodcastData
    {
        public Guid Id { get; set; }
    }
}
