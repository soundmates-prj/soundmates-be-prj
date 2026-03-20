using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Infrastructure.Integrations.Services
{
    public class UserServiceClient
    {
        //private readonly HttpClient _httpClient;

        //public UserServiceClient(IHttpClientFactory factory)
        //{
        //    _httpClient = factory.CreateClient("UserService");
        //}

        //public async Task<UserProfileResponse> GetUserProfile(Guid userId)
        //{
        //    var response = await _httpClient.GetAsync($"api/users/{userId}");

        //    if (!response.IsSuccessStatusCode)
        //    {
        //        throw new Exception("Cannot fetch user profile");
        //    }

        //    var content = await response.Content.ReadAsStringAsync();

        //    return JsonConvert.DeserializeObject<UserProfileResponse>(content);
        //}
    }
}
