using AccountContentService.Application.DTOs;
using AccountContentService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AccountContentService.Infrastructure.Integrations.Services
{
    public class UserServiceClient : IUserServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UserServiceClient> _logger;

        public UserServiceClient(
            IHttpClientFactory factory,
            IHttpContextAccessor httpContextAccessor,
            ILogger<UserServiceClient> logger)
        {
            _httpClient = factory.CreateClient("ApiGateway");
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<UserProfileDto> GetMyProfile()
        {
            var token = _httpContextAccessor.HttpContext?
                .Request.Headers["Authorization"]
                .ToString();

            if (string.IsNullOrEmpty(token))
                throw new UnauthorizedAccessException("Missing Authorization token.");

            // 1. Create a scoped request message instead of mutating the global HttpClient
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/users/me/profile/full");

            // 2. Safely parse and attach the token to this specific request
            if (AuthenticationHeaderValue.TryParse(token, out var parsedToken))
            {
                request.Headers.Authorization = parsedToken;
            }
            else
            {
                // Fallback in case the raw header is needed
                request.Headers.TryAddWithoutValidation("Authorization", token);
            }

            // 3. Send the request
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var rawError = await response.Content.ReadAsStringAsync();
                _logger.LogError("User service error: {Status} - {Body}", response.StatusCode, rawError);

                throw new HttpRequestException($"User service returned an error: {response.StatusCode}");
            }

            var result = await response.Content
                .ReadFromJsonAsync<ApiResponseWrapper<UserProfileDto>>();

            var data = result?.Data
                ?? throw new InvalidOperationException("User profile data was missing");

            // ✅ Log đẹp
            _logger.LogInformation(
    "Fetched user profile: {UserProfile}",
    JsonSerializer.Serialize(data, new JsonSerializerOptions
    {
        WriteIndented = true
    })
);

            return data;
        }

        public record ApiResponseWrapper<T>(T Data);

        public async Task<UserProfileDto> GetProfileByUserId(Guid userId)
        {
            var token = _httpContextAccessor.HttpContext?
               .Request.Headers["Authorization"]
               .ToString();

            if (string.IsNullOrEmpty(token))
                throw new Exception("Missing Authorization token");

            _httpClient.DefaultRequestHeaders.Authorization =
                AuthenticationHeaderValue.Parse(token);

            var response = await _httpClient.GetAsync($"api/v1/users/{userId}/profile/full");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"User service error: {response.StatusCode}");
            }

            return await response.Content.ReadFromJsonAsync<UserProfileDto>();
        }
    }
}