using AiService.Application.Interfaces;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace AiService.Infrastructure.Clients;

public class VieNeuTtsClient : ITtsClient
{
    private readonly HttpClient _http;
    private readonly TtsOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public VieNeuTtsClient(HttpClient http, IOptions<TtsOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<TtsSynthesizeResponse> SynthesizeAsync(TtsSynthesizeRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl) || _options.BaseUrl.StartsWith("${"))
            throw new InvalidOperationException("TTS_BASE_URL is required for VieNeuTTS sync client.");

        var synthesizePath = string.IsNullOrWhiteSpace(_options.SynthesizePath)
            ? "/v1/tts/synthesize"
            : _options.SynthesizePath!;

        var payload = new Dictionary<string, object?>
        {
            ["text"] = request.Text,
            ["voice"] = request.VoiceCode,
            ["speed"] = request.Speed,
            ["pitch"] = request.Pitch,
            ["model"] = _options.Model,
            ["format"] = string.IsNullOrWhiteSpace(_options.AudioFormat) ? "mp3" : _options.AudioFormat,
            ["prompt"] = BuildPrompt(request)
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildUri(_options.BaseUrl, synthesizePath))
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };

        if (!string.IsNullOrWhiteSpace(_options.ApiKey) && !_options.ApiKey.StartsWith("${"))
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }

        using var response = await _http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (!string.IsNullOrWhiteSpace(mediaType) && mediaType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return new TtsSynthesizeResponse(bytes, mediaType);
        }

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(raw);

        var root = document.RootElement;
        var audioBase64 =
            TryGetString(root, "audioBase64") ??
            TryGetString(root, "audio_base64") ??
            TryGetString(root, "audio") ??
            TryGetNestedString(root, "data", "audioBase64") ??
            TryGetNestedString(root, "data", "audio_base64") ??
            TryGetNestedString(root, "result", "audioBase64") ??
            TryGetNestedString(root, "result", "audio_base64");

        if (string.IsNullOrWhiteSpace(audioBase64))
            throw new InvalidOperationException("VieNeuTTS response does not contain audio payload.");

        var normalizedBase64 = ExtractBase64(audioBase64);
        var audioBytes = Convert.FromBase64String(normalizedBase64);

        var contentType =
            TryGetString(root, "contentType") ??
            TryGetString(root, "content_type") ??
            TryGetNestedString(root, "data", "contentType") ??
            TryGetNestedString(root, "data", "content_type") ??
            GuessContentType(_options.AudioFormat);

        var duration =
            TryGetInt(root, "durationSeconds") ??
            TryGetInt(root, "duration_seconds") ??
            TryGetNestedInt(root, "data", "durationSeconds") ??
            TryGetNestedInt(root, "data", "duration_seconds");

        return new TtsSynthesizeResponse(
            AudioBytes: audioBytes,
            ContentType: contentType,
            DurationSeconds: duration,
            RawProviderResponse: raw);
    }

    private static string BuildUri(string baseUrl, string path)
    {
        if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
            return path;

        return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }

    private string? BuildPrompt(TtsSynthesizeRequest request)
    {
        if (string.IsNullOrWhiteSpace(_options.PromptTemplate))
            return null;

        return _options.PromptTemplate
            .Replace("{text}", request.Text, StringComparison.Ordinal)
            .Replace("{voice}", request.VoiceCode, StringComparison.Ordinal)
            .Replace("{speed}", request.Speed?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, StringComparison.Ordinal)
            .Replace("{pitch}", request.Pitch?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, StringComparison.Ordinal);
    }

    private static string ExtractBase64(string value)
    {
        const string marker = "base64,";
        var idx = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? value[(idx + marker.Length)..] : value;
    }

    private static string GuessContentType(string? configuredFormat)
    {
        if (string.Equals(configuredFormat, "wav", StringComparison.OrdinalIgnoreCase))
            return "audio/wav";

        return "audio/mpeg";
    }

    private static string? TryGetString(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string? TryGetNestedString(JsonElement root, string parentProperty, string childProperty)
    {
        if (!root.TryGetProperty(parentProperty, out var parent) || parent.ValueKind != JsonValueKind.Object)
            return null;

        return TryGetString(parent, childProperty);
    }

    private static int? TryGetInt(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
            return null;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var i))
            return i;

        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
            return parsed;

        return null;
    }

    private static int? TryGetNestedInt(JsonElement root, string parentProperty, string childProperty)
    {
        if (!root.TryGetProperty(parentProperty, out var parent) || parent.ValueKind != JsonValueKind.Object)
            return null;

        return TryGetInt(parent, childProperty);
    }
}

