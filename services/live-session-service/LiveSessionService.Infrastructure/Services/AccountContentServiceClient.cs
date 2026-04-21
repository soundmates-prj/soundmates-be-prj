using System.Net.Http.Headers;
using System.Text.Json;
using LiveSessionService.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Infrastructure.Services;

public class AccountContentServiceClient : IAccountContentClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AccountContentServiceClient> _logger;

    public AccountContentServiceClient(HttpClient httpClient, ILogger<AccountContentServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UserSubscriptionDto?> GetMySubscriptionFullAsync(string userToken, CancellationToken ct = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/me/subscriptions/full");
            
            // Forward the JWT token
            if (!string.IsNullOrWhiteSpace(userToken))
            {
                var tokenValue = userToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) 
                    ? userToken.Substring("Bearer ".Length).Trim() 
                    : userToken;
                
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenValue);
            }

            var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch subscription bounds. Status code: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<ApiResponse<UserSubscriptionDto>>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            return result?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching subscription bounds from Account Content Service.");
            return null;
        }
    }

    private class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public int StatusCode { get; set; }
    }
}
