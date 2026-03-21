using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AuthQueryService.Application.Abstractions;
using AuthQueryService.Application.DTOs.Spotify;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthQueryService.Infrastructure.ExternalServices
{
    /// <summary>
    /// Production Spotify API client using Client Credentials Flow.
    ///
    /// Thread-safe token caching via SemaphoreSlim.
    /// Automatic 401 retry + rate-limit handling.
    /// Register as Singleton via AddHttpClient&lt;ISpotifyApiClient, SpotifyApiClient&gt;()
    /// so the token is cached for the lifetime of the process.
    /// </summary>
    public sealed class SpotifyApiClient : ISpotifyApiClient, IDisposable
    {
        private readonly string _tokenEndpoint;
        private readonly string _apiBase;
        private const int TokenBufferSeconds = 60;

        private readonly HttpClient _http;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly ILogger<SpotifyApiClient> _logger;

        private readonly SemaphoreSlim _tokenLock = new(1, 1);
        private string? _accessToken;
        private DateTime _tokenExpiresAtUtc = DateTime.MinValue;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public SpotifyApiClient(
            HttpClient http,
            IConfiguration configuration,
            ILogger<SpotifyApiClient> logger)
        {
            _http = http;
            _logger = logger;

            _clientId = configuration["Spotify:ClientId"]
                ?? throw new InvalidOperationException("Missing: Spotify:ClientId");
            _clientSecret = configuration["Spotify:ClientSecret"]
                ?? throw new InvalidOperationException("Missing: Spotify:ClientSecret");
            
            _tokenEndpoint = configuration["Spotify:TokenEndpoint"] ?? "https://accounts.spotify.com/api/token";
            _apiBase = configuration["Spotify:ApiBase"] ?? "https://api.spotify.com/v1";
        }

        // ── Public surface ───────────────────────────────────────────────

        public async Task<SpotifySearchResult> SearchAsync(
            string query, string type = "track", int limit = 10, int offset = 0, CancellationToken ct = default, string? userAccessToken = null)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Search query is required.", nameof(query));

            // Clamp to Spotify-accepted range
            // NOTE: Spotify Developer Mode apps are capped at limit ≤ 10.
            //       Increase to 50 only after requesting quota extension in Spotify Dashboard.
            limit  = Math.Clamp(limit, 1, 10);
            offset = Math.Max(0, offset);

            // Normalize type: trim whitespace, lower-case, allow comma-separated values
            // e.g. "track,artist,album" — Spotify expects raw comma, NOT %2C
            var normalizedType = string.Join(",",
                type.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => t.Trim().ToLowerInvariant()));
            if (string.IsNullOrWhiteSpace(normalizedType))
                normalizedType = "track";

            var url = $"{_apiBase}/search?q={Uri.EscapeDataString(query)}&type={normalizedType}&limit={limit}&offset={offset}";

            _logger.LogInformation("Spotify search → {Url}", url);
            return await GetAsync<SpotifySearchResult>(url, ct, userAccessToken) ?? new SpotifySearchResult();
        }

        public async Task<SpotifyTrackDto?> GetTrackAsync(string trackId, CancellationToken ct = default)
        {
            GuardId(trackId, nameof(trackId));
            return await GetAsync<SpotifyTrackDto>($"{_apiBase}/tracks/{Uri.EscapeDataString(trackId)}", ct);
        }

        public async Task<SpotifyArtistDto?> GetArtistAsync(string artistId, CancellationToken ct = default)
        {
            GuardId(artistId, nameof(artistId));
            return await GetAsync<SpotifyArtistDto>($"{_apiBase}/artists/{Uri.EscapeDataString(artistId)}", ct);
        }

        public async Task<SpotifyAlbumDto?> GetAlbumAsync(string albumId, CancellationToken ct = default)
        {
            GuardId(albumId, nameof(albumId));
            return await GetAsync<SpotifyAlbumDto>($"{_apiBase}/albums/{Uri.EscapeDataString(albumId)}", ct);
        }

        public async Task<List<SpotifyTrackDto>> GetSeveralTracksAsync(
            IEnumerable<string> trackIds, CancellationToken ct = default)
        {
            var ids = (trackIds ?? throw new ArgumentNullException(nameof(trackIds)))
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Take(50)
                .ToList();

            if (ids.Count == 0) return [];

            var joined = string.Join(",", ids.Select(Uri.EscapeDataString));
            var result = await GetAsync<SpotifySeveralTracksResult>($"{_apiBase}/tracks?ids={joined}", ct);
            return result?.Tracks ?? [];
        }

        // ── HTTP helpers ─────────────────────────────────────────────────

        private async Task<T?> GetAsync<T>(string url, CancellationToken ct, string? userAccessToken = null) where T : class
        {
            if (string.IsNullOrEmpty(userAccessToken))
            {
                await EnsureTokenAsync(ct);
            }

            var tokenToUse = string.IsNullOrEmpty(userAccessToken) ? _accessToken : userAccessToken;

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenToUse);

            using var resp = await _http.SendAsync(req, ct);

            // Only retry for server token (Client Credentials)
            if (resp.StatusCode == HttpStatusCode.Unauthorized && string.IsNullOrEmpty(userAccessToken))
            {
                _logger.LogWarning("Spotify 401 — refreshing token and retrying.");
                await RefreshTokenAsync(ct);

                using var retry = new HttpRequestMessage(HttpMethod.Get, url);
                retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                using var retryResp = await _http.SendAsync(retry, ct);
                return await ReadResponseAsync<T>(retryResp, url, ct);
            }
            
            // If user token is unauthorized, throw SpotifyApiException to pass it out
            if (resp.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(userAccessToken))
            {
                throw new SpotifyApiException(401, "User access token expired or invalid");
            }

            return await ReadResponseAsync<T>(resp, url, ct);
        }

        private async Task<T?> ReadResponseAsync<T>(HttpResponseMessage resp, string url, CancellationToken ct)
            where T : class
        {
            if (resp.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Spotify 404 for {Url}", url);
                return null;
            }

            if (resp.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = resp.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(5);
                throw new SpotifyRateLimitException(retryAfter);
            }

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogError("Spotify error {Status}: {Body}", (int)resp.StatusCode, body);
                throw new SpotifyApiException((int)resp.StatusCode, body);
            }

            return await resp.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
        }

        // ── Token management ─────────────────────────────────────────────

        private async Task EnsureTokenAsync(CancellationToken ct)
        {
            if (_accessToken is not null && DateTime.UtcNow < _tokenExpiresAtUtc)
                return;
            await RefreshTokenAsync(ct);
        }

        private async Task RefreshTokenAsync(CancellationToken ct)
        {
            await _tokenLock.WaitAsync(ct);
            try
            {
                if (_accessToken is not null && DateTime.UtcNow < _tokenExpiresAtUtc) return;

                _logger.LogInformation("Requesting new Spotify client credentials token.");

                var creds = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));

                using var req = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint);
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", creds);
                req.Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                });

                using var resp = await _http.SendAsync(req, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    var err = await resp.Content.ReadAsStringAsync(ct);
                    throw new SpotifyApiException((int)resp.StatusCode, err);
                }

                var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
                _accessToken = json.GetProperty("access_token").GetString()!;
                var expiresIn = json.GetProperty("expires_in").GetInt32();
                _tokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(expiresIn - TokenBufferSeconds);

                _logger.LogInformation("Spotify token acquired. Valid for {S}s.", expiresIn - TokenBufferSeconds);
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        private static void GuardId(string id, string paramName)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID cannot be empty.", paramName);
        }

        public void Dispose() => _tokenLock.Dispose();
    }

    public class SpotifyApiException(int statusCode, string? body)
        : Exception($"Spotify API error {statusCode}.")
    {
        public int StatusCode { get; } = statusCode;
        public string? ResponseBody { get; } = body;
    }

    public sealed class SpotifyRateLimitException(TimeSpan retryAfter)
        : SpotifyApiException(429, null)
    {
        public TimeSpan RetryAfter { get; } = retryAfter;
    }
}
