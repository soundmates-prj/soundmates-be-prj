using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AuthService.Application.Abstractions;
using AuthService.Application.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.ExternalServices;

/// <summary>
/// Spotify Web API client (Client Credentials Flow) for auth-service.
///
/// Configuration is injected via <see cref="IOptions{SpotifyOptions}"/> — no hardcoded URLs.
/// Thread-safe token caching via <see cref="SemaphoreSlim"/>.
/// Register as Singleton via AddHttpClient so the cached token is shared across requests.
///
/// Non-throwing design: all public methods return null on Spotify error so the
/// favourite write path (PostgreSQL) never fails due to metadata enrichment.
/// </summary>
public sealed class SpotifyApiClient : ISpotifyApiClient, IDisposable
{
    private readonly HttpClient            _http;
    private readonly SpotifyOptions        _opts;
    private readonly ILogger<SpotifyApiClient> _logger;

    private readonly SemaphoreSlim _lock      = new(1, 1);
    private string?  _accessToken;
    private DateTime _tokenExpiresAt          = DateTime.MinValue;

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public SpotifyApiClient(
        HttpClient              http,
        IOptions<SpotifyOptions> options,
        ILogger<SpotifyApiClient> logger)
    {
        _http   = http;
        _opts   = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_opts.ClientId))
            throw new InvalidOperationException(
                "Spotify:ClientId is not configured. " +
                "Set the SPOTIFY_CLIENT_ID environment variable or add Spotify:ClientId to appsettings.");

        if (string.IsNullOrWhiteSpace(_opts.ClientSecret))
            throw new InvalidOperationException(
                "Spotify:ClientSecret is not configured. " +
                "Set the SPOTIFY_CLIENT_SECRET environment variable or add Spotify:ClientSecret to appsettings.");
    }

    // ── ISpotifyApiClient ────────────────────────────────────────────────────

    public async Task<SpotifyTrackInfo?> GetTrackAsync(string trackId, CancellationToken ct = default)
    {
        var doc = await GetAsync($"{_opts.ApiBase}/tracks/{Uri.EscapeDataString(trackId)}", ct);
        if (doc is null) return null;

        var name       = doc.Value.GetStringOrEmpty("name");
        var artistName = doc.Value.GetFirstArrayString("artists", "name");
        var album      = doc.Value.TryGetProperty("album", out var alb)
                            ? alb : (JsonElement?)null;
        var albumName  = album?.GetStringOrEmpty("name") ?? string.Empty;
        var imgUrl     = album?.GetImageUrl() ?? doc.Value.GetImageUrl();
        var preview    = doc.Value.GetNullableString("preview_url");
        var duration   = doc.Value.TryGetProperty("duration_ms", out var dur) ? dur.GetInt32() : 0;
        var extUrl     = doc.Value.GetExternalSpotifyUrl();

        return new SpotifyTrackInfo(trackId, name, artistName, albumName, imgUrl, preview, duration, extUrl);
    }

    public async Task<SpotifyArtistInfo?> GetArtistAsync(string artistId, CancellationToken ct = default)
    {
        var doc = await GetAsync($"{_opts.ApiBase}/artists/{Uri.EscapeDataString(artistId)}", ct);
        if (doc is null) return null;

        return new SpotifyArtistInfo(
            artistId,
            doc.Value.GetStringOrEmpty("name"),
            doc.Value.GetImageUrl(),
            doc.Value.GetExternalSpotifyUrl());
    }

    public async Task<SpotifyAlbumInfo?> GetAlbumAsync(string albumId, CancellationToken ct = default)
    {
        var doc = await GetAsync($"{_opts.ApiBase}/albums/{Uri.EscapeDataString(albumId)}", ct);
        if (doc is null) return null;

        return new SpotifyAlbumInfo(
            albumId,
            doc.Value.GetStringOrEmpty("name"),
            doc.Value.GetFirstArrayString("artists", "name"),
            doc.Value.GetImageUrl(),
            doc.Value.GetExternalSpotifyUrl());
    }

    // ── HTTP internals ────────────────────────────────────────────────────────

    private async Task<JsonElement?> GetAsync(string url, CancellationToken ct)
    {
        await EnsureTokenAsync(ct);

        using var req  = BuildGet(url);
        using var resp = await _http.SendAsync(req, ct);

        if (resp.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Spotify 401 — refreshing token and retrying once.");
            await RefreshTokenAsync(ct);
            using var retry     = BuildGet(url);
            using var retryResp = await _http.SendAsync(retry, ct);
            return await ReadBodyAsync(retryResp, url, ct);
        }

        return await ReadBodyAsync(resp, url, ct);
    }

    private HttpRequestMessage BuildGet(string url)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        return req;
    }

    private async Task<JsonElement?> ReadBodyAsync(
        HttpResponseMessage resp, string url, CancellationToken ct)
    {
        if (resp.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogDebug("Spotify 404: {Url}", url);
            return null;
        }

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            // Non-fatal — the write path must not fail because of enrichment
            _logger.LogWarning("Spotify {Status} for {Url}: {Body}",
                (int)resp.StatusCode, url, body);
            return null;
        }

        return await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts, ct);
    }

    // ── Token management ──────────────────────────────────────────────────────

    private async Task EnsureTokenAsync(CancellationToken ct)
    {
        if (_accessToken is not null && DateTime.UtcNow < _tokenExpiresAt) return;
        await RefreshTokenAsync(ct);
    }

    private async Task RefreshTokenAsync(CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock (another thread may have refreshed)
            if (_accessToken is not null && DateTime.UtcNow < _tokenExpiresAt) return;

            _logger.LogInformation("Requesting Spotify client credentials token.");

            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_opts.ClientId}:{_opts.ClientSecret}"));

            using var req = new HttpRequestMessage(HttpMethod.Post, _opts.TokenEndpoint);
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            req.Content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            ]);

            using var resp = await _http.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();

            var json       = await resp.Content.ReadFromJsonAsync<JsonElement>(
                                 cancellationToken: ct);
            _accessToken   = json.GetProperty("access_token").GetString()!;
            var expiresIn  = json.GetProperty("expires_in").GetInt32();
            _tokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn - _opts.TokenBufferSeconds);

            _logger.LogInformation(
                "Spotify token acquired. Valid for {Seconds}s.",
                expiresIn - _opts.TokenBufferSeconds);
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose() => _lock.Dispose();
}

// ── JsonElement extension methods ─────────────────────────────────────────────
// Keeps the client code expressive without stringly-typed repetition.

internal static class JsonElementSpotifyExtensions
{
    /// <summary>Get a string property, returning empty string if absent/null.</summary>
    public static string GetStringOrEmpty(this JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? string.Empty
            : string.Empty;

    /// <summary>Get a nullable string property.</summary>
    public static string? GetNullableString(this JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    /// <summary>Get first element of an array property, then read a sub-property.</summary>
    public static string GetFirstArrayString(this JsonElement el, string arrayProp, string subProp)
    {
        if (!el.TryGetProperty(arrayProp, out var arr)) return string.Empty;
        if (arr.ValueKind != JsonValueKind.Array || arr.GetArrayLength() == 0) return string.Empty;
        return arr[0].GetStringOrEmpty(subProp);
    }

    /// <summary>Get the largest image URL from a Spotify "images" array.</summary>
    public static string? GetImageUrl(this JsonElement el)
    {
        if (!el.TryGetProperty("images", out var imgs)) return null;
        if (imgs.ValueKind != JsonValueKind.Array || imgs.GetArrayLength() == 0) return null;
        return imgs[0].GetNullableString("url");
    }

    /// <summary>Get the Spotify external URL from external_urls.spotify.</summary>
    public static string? GetExternalSpotifyUrl(this JsonElement el)
    {
        if (!el.TryGetProperty("external_urls", out var ext)) return null;
        return ext.GetNullableString("spotify");
    }
}
