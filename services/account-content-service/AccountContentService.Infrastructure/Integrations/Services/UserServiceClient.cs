using AccountContentService.Application.DTOs;
using AccountContentService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AccountContentService.Infrastructure.Integrations.Services
{
    public class UserServiceClient : IUserServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserServiceClient(
            IHttpClientFactory factory,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = factory.CreateClient("ApiGateway");
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<UserProfileDto> GetMyProfile()
        {
            var token = _httpContextAccessor.HttpContext?
                .Request.Headers["Authorization"]
                .ToString();

            if (string.IsNullOrEmpty(token))
                throw new Exception("Missing Authorization token");

            _httpClient.DefaultRequestHeaders.Authorization =
                AuthenticationHeaderValue.Parse(token);

            var response = await _httpClient.GetAsync("api/v1/users/me/profile/full");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"User service error: {response.StatusCode}");
            }

            return await response.Content.ReadFromJsonAsync<UserProfileDto>();
        }

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