using AccountContentService.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AccountContentService.Infrastructure.Integrations.Services;

public class AuthApiClient : IAuthApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthApiClient> _logger;

    public AuthApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<AuthApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        var baseUrl = configuration["AuthService:BaseUrl"] ?? "http://localhost:5001";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<BankAccountDto?> GetUserBankAccountAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/users/{userId}/bank-account", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<BankAccountDto>(cancellationToken: cancellationToken);
                return result;
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Bank account not found for user {UserId}", userId);
                return null;
            }

            _logger.LogWarning("Failed to fetch bank account for {UserId}. Status: {StatusCode}", userId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching bank account for {UserId}", userId);
            return null;
        }
    }

    public async Task<List<Guid>> GetAdminUserIdsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/v1/users/admins?page=1&pageSize=100", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<System.Text.Json.Nodes.JsonObject>(cancellationToken: cancellationToken);
                var items = result?["data"]?["items"]?.AsArray();
                
                if (items != null)
                {
                    var ids = new System.Collections.Generic.List<Guid>();
                    foreach (var item in items)
                    {
                        if (item?["id"] != null && Guid.TryParse(item["id"]!.ToString(), out var id))
                        {
                            ids.Add(id);
                        }
                    }
                    return ids;
                }
            }
            
            _logger.LogWarning("Failed to fetch admin users. Status: {StatusCode}", response.StatusCode);
            return new System.Collections.Generic.List<Guid>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching admin users");
            return new System.Collections.Generic.List<Guid>();
        }
    }
}
