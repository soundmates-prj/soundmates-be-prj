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
}
