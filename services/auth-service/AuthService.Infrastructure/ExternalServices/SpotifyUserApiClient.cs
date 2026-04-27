using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AuthService.Application.Abstractions;
using AuthService.Application.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.ExternalServices;

public sealed class SpotifyUserApiClient : ISpotifyUserApiClient
{
    private readonly HttpClient _http;
    private readonly SpotifyOptions _opts;
    private readonly ILogger<SpotifyUserApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public SpotifyUserApiClient(HttpClient http, IOptions<SpotifyOptions> options, ILogger<SpotifyUserApiClient> logger)
    {
        _http = http;
        _opts = options.Value;
        _logger = logger;
    }

    public async Task<SpotifyOAuthTokenResponse?> ExchangeCodeAsync(string code, string redirectUri, CancellationToken ct = default)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_opts.ClientId}:{_opts.ClientSecret}"));

        using var req = new HttpRequestMessage(HttpMethod.Post, _opts.TokenEndpoint);
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        req.Content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", redirectUri)
        ]);

        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogError("Spotify ExchangeCode error: {Err}", err);
            return null;
        }

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts, ct);
        return new SpotifyOAuthTokenResponse(
            json.GetProperty("access_token").GetString()!,
            json.GetProperty("token_type").GetString()!,
            json.GetProperty("expires_in").GetInt32(),
            json.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null,
            json.TryGetProperty("scope", out var s) ? s.GetString() : null
        );
    }

    public async Task<SpotifyOAuthTokenResponse?> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_opts.ClientId}:{_opts.ClientSecret}"));

        using var req = new HttpRequestMessage(HttpMethod.Post, _opts.TokenEndpoint);
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        req.Content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        ]);

        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogError("Spotify RefreshToken error: {Err}", err);
            return null;
        }

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts, ct);
        return new SpotifyOAuthTokenResponse(
            json.GetProperty("access_token").GetString()!,
            json.GetProperty("token_type").GetString()!,
            json.GetProperty("expires_in").GetInt32(),
            json.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : refreshToken,
            json.TryGetProperty("scope", out var s) ? s.GetString() : null
        );
    }

    public async Task<SpotifyUserProfile?> GetUserProfileAsync(string accessToken, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{_opts.ApiBase}/me");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogError("Spotify GetUserProfile error: {Err}", err);
            return null;
        }

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts, ct);
        
        var images = json.TryGetProperty("images", out var imgs) && imgs.ValueKind == JsonValueKind.Array && imgs.GetArrayLength() > 0 ? imgs[0].GetNullableString("url") : null;
        
        return new SpotifyUserProfile(
            json.GetProperty("id").GetString()!,
            json.GetStringOrEmpty("display_name"),
            json.GetStringOrEmpty("email"),
            json.GetNullableString("country"),
            json.GetNullableString("product"),
            json.GetNullableString("uri"),
            images
        );
    }
}
