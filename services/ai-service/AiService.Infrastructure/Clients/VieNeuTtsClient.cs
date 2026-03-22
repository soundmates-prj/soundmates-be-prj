using AiService.Application.Interfaces;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Buffers.Binary;

namespace AiService.Infrastructure.Clients;

public class VieNeuTtsClient : ITtsClient
{
    private readonly HttpClient _http;
    private readonly TtsOptions _options;
    private const int EndpointModeAuto = 0;
    private const int EndpointModeSynthesize = 1;
    private const int EndpointModeStream = 2;
    private int _endpointMode = EndpointModeAuto;
    private string? _streamBaseUrlOverride;
    private readonly ConcurrentDictionary<string, byte> _streamProbeFailures = new(StringComparer.OrdinalIgnoreCase);
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
        var baseUrl = _options.BaseUrl;

        var synthesizePath = string.IsNullOrWhiteSpace(_options.SynthesizePath)
            ? "/v1/tts/synthesize"
            : _options.SynthesizePath!;

        // Short-circuit using the previously discovered working endpoint.
        if (Volatile.Read(ref _endpointMode) == EndpointModeStream)
        {
            var streamResponse = await TrySynthesizeViaStreamAsync(baseUrl, request, cancellationToken);
            if (streamResponse is not null)
                return streamResponse;
        }

        if (string.Equals(synthesizePath, "/v1/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            return await SynthesizeViaChatCompletionsAsync(request, cancellationToken);
        }

        if (string.Equals(synthesizePath, "/stream", StringComparison.OrdinalIgnoreCase))
        {
            var streamResponse = await TrySynthesizeViaStreamAsync(baseUrl, request, cancellationToken);
            if (streamResponse is null)
                throw new InvalidOperationException("VieNeu /stream endpoint is configured but did not return audio. Ensure apps/web_stream.py is running and reachable from ai-service.");

            Volatile.Write(ref _endpointMode, EndpointModeStream);
            return streamResponse;
        }

        Volatile.Write(ref _endpointMode, EndpointModeSynthesize);

        var payload = new Dictionary<string, object?>
        {
            ["text"] = request.Text,
            ["voice"] = request.VoiceCode,
            ["speed"] = request.Speed,
            ["pitch"] = request.Pitch,
            ["model"] = string.IsNullOrWhiteSpace(request.Model) || request.Model.StartsWith("${")
                ? _options.Model
                : request.Model,
            ["format"] = string.IsNullOrWhiteSpace(_options.AudioFormat) ? "wav" : _options.AudioFormat,
            ["prompt"] = BuildPrompt(request)
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildUri(baseUrl, synthesizePath))
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };

        if (!string.IsNullOrWhiteSpace(_options.ApiKey) && !_options.ApiKey.StartsWith("${"))
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await _http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            // Some deployments expose /stream instead of /v1/tts/synthesize.
            var streamResponse = await TrySynthesizeViaStreamAsync(baseUrl, request, cancellationToken);
            if (streamResponse is not null)
            {
                Volatile.Write(ref _endpointMode, EndpointModeStream);
                return streamResponse;
            }

            throw new InvalidOperationException(
                "No audio synthesis endpoint found. '/v1/tts/synthesize' returned 404 and '/stream' is unavailable. " +
                "The current TTS server likely runs as text-only LLM API. " +
                "Please run a sync audio server and configure TTS_BASE_URL + TTS_SYNTHESIZE_PATH=/stream.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await ReadErrorBodyAsync(response, cancellationToken);
            throw new HttpRequestException(
                $"VieNeuTTS synthesize endpoint failed with {(int)response.StatusCode} ({response.ReasonPhrase}). " +
                $"url='{BuildUri(baseUrl, synthesizePath)}' body='{errorBody}'");
        }

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (!string.IsNullOrWhiteSpace(mediaType) && mediaType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var normalized = NormalizeWavHeaderIfNeeded(bytes, mediaType);
            var audioDuration = EstimateDurationFromAudio(normalized, mediaType, request.Text);
            return new TtsSynthesizeResponse(normalized, mediaType, DurationSeconds: audioDuration);
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

        // Fallback: estimate duration from audio bytes or text if API didn't return duration
        var finalDuration = duration ?? EstimateDurationFromAudio(audioBytes, contentType, request.Text);

        return new TtsSynthesizeResponse(
            AudioBytes: audioBytes,
            ContentType: contentType,
            DurationSeconds: finalDuration,
            RawProviderResponse: raw);
    }

    private async Task<TtsSynthesizeResponse> SynthesizeViaChatCompletionsAsync(TtsSynthesizeRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl) || _options.BaseUrl.StartsWith("${"))
            throw new InvalidOperationException("TTS_BASE_URL is required for VieNeuTTS sync client.");
        var baseUrl = _options.BaseUrl;

        var ttsModel = string.IsNullOrWhiteSpace(request.Model) || request.Model.StartsWith("${")
            ? _options.Model
            : request.Model;

        var payload = new
        {
            model = ttsModel,
            messages = new[]
            {
                new { role = "user", content = BuildChatPrompt(request) }
            },
            max_tokens = 2048,
            temperature = 1.0,
            top_k = 50,
            stop = new[] { "<|SPEECH_GENERATION_END|>" },
            stream = false
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildUri(baseUrl, "/v1/chat/completions"))
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };

        if (!string.IsNullOrWhiteSpace(_options.ApiKey) && !_options.ApiKey.StartsWith("${"))
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await _http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await ReadErrorBodyAsync(response, cancellationToken);
            throw new HttpRequestException(
                $"VieNeuTTS chat-completions endpoint failed with {(int)response.StatusCode} ({response.ReasonPhrase}). " +
                $"url='{BuildUri(baseUrl, "/v1/chat/completions")}' body='{errorBody}'");
        }

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(raw);

        if (!TryParseAudioFromChatResponse(document.RootElement, out var audioBytes, out var contentType, out var durationSeconds))
        {
            var preview = raw.Replace("\r", " ").Replace("\n", " ").Trim();
            if (preview.Length > 300)
                preview = preview[..300];

            var modelHint = TryGetModelHint(document.RootElement);
            throw new InvalidOperationException(
                "VieNeuTTS chat completion response did not contain decodable audio payload. " +
                "This usually means the configured endpoint is an LMDeploy text/token endpoint (not sync audio output). " +
                $"model='{modelHint ?? "unknown"}' raw='{preview}'. " +
                "Use a VieNeu sync audio endpoint (/v1/tts/synthesize) or stream endpoint (/stream) that returns audio bytes.");
        }

        // Fallback: estimate duration from audio bytes or text if API didn't return duration
        var finalDuration = durationSeconds ?? EstimateDurationFromAudio(audioBytes, contentType, request.Text);
        return new TtsSynthesizeResponse(
            AudioBytes: audioBytes,
            ContentType: contentType,
            DurationSeconds: finalDuration,
            RawProviderResponse: raw);
    }

    private string BuildChatPrompt(TtsSynthesizeRequest request)
    {
        var templated = BuildPrompt(request);
        if (!string.IsNullOrWhiteSpace(templated))
            return templated;

        return
            $"Tạo audio từ text tiếng Việt. " +
            $"Text: '{request.Text}'. Voice: '{request.VoiceCode}'. " +
            $"Speed: {request.Speed?.ToString(CultureInfo.InvariantCulture)}. " +
            $"Pitch: {request.Pitch?.ToString(CultureInfo.InvariantCulture)}. " +
            $"Yêu cầu: trả về JSON {{\"audioBase64\":\"<base64>\"}}.";
    }

    private bool TryParseAudioFromChatContent(string content, out byte[] audioBytes, out string contentType, out int? durationSeconds)
    {
        audioBytes = Array.Empty<byte>();
        contentType = GuessContentType(_options.AudioFormat);
        durationSeconds = null;

        try
        {
            using var contentDoc = JsonDocument.Parse(content);
            var root = contentDoc.RootElement;

            var audioBase64 =
                TryGetString(root, "audioBase64") ??
                TryGetString(root, "audio_base64") ??
                TryGetString(root, "audio") ??
                TryGetNestedString(root, "data", "audioBase64") ??
                TryGetNestedString(root, "data", "audio_base64") ??
                TryGetNestedString(root, "result", "audioBase64") ??
                TryGetNestedString(root, "result", "audio_base64");

            if (string.IsNullOrWhiteSpace(audioBase64))
                return false;

            audioBytes = Convert.FromBase64String(ExtractBase64(audioBase64));

            contentType =
                TryGetString(root, "contentType") ??
                TryGetString(root, "content_type") ??
                TryGetNestedString(root, "data", "contentType") ??
                TryGetNestedString(root, "data", "content_type") ??
                GuessContentType(_options.AudioFormat);

            durationSeconds =
                TryGetInt(root, "durationSeconds") ??
                TryGetInt(root, "duration_seconds") ??
                TryGetNestedInt(root, "data", "durationSeconds") ??
                TryGetNestedInt(root, "data", "duration_seconds");

            return audioBytes.Length > 0;
        }
        catch (JsonException)
        {
            var normalized = ExtractBase64(content.Trim());
            if (!LooksLikeBase64(normalized))
                return false;

            try
            {
                audioBytes = Convert.FromBase64String(normalized);
                contentType = GuessContentType(_options.AudioFormat);
                return audioBytes.Length > 0;
            }
            catch (FormatException)
            {
                return false;
            }
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private bool TryParseAudioFromChatResponse(JsonElement root, out byte[] audioBytes, out string contentType, out int? durationSeconds)
    {
        audioBytes = Array.Empty<byte>();
        contentType = GuessContentType(_options.AudioFormat);
        durationSeconds = null;

        // 1) Common OpenAI-style locations: choices[0].message.audio.* or choices[0].audio.*
        if (TryGetFirstChoice(root, out var choice))
        {
            if (TryGetNestedObject(choice, "message", out var message))
            {
                if (TryParseAudioFromElement(message, out audioBytes, out contentType, out durationSeconds))
                    return true;

                if (TryGetString(message, "content") is string content &&
                    TryParseAudioFromChatContent(content, out audioBytes, out contentType, out durationSeconds))
                    return true;

                if (TryGetProperty(message, "content", out var contentElement) &&
                    contentElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var part in contentElement.EnumerateArray())
                    {
                        if (TryParseAudioFromElement(part, out audioBytes, out contentType, out durationSeconds))
                            return true;

                        if (TryGetString(part, "text") is string partText &&
                            TryParseAudioFromChatContent(partText, out audioBytes, out contentType, out durationSeconds))
                            return true;
                    }
                }
            }

            if (TryParseAudioFromElement(choice, out audioBytes, out contentType, out durationSeconds))
                return true;
        }

        // 2) Fallback: provider may place audio directly at top-level.
        if (TryParseAudioFromElement(root, out audioBytes, out contentType, out durationSeconds))
            return true;

        return false;
    }

    private bool TryParseAudioFromElement(JsonElement element, out byte[] audioBytes, out string contentType, out int? durationSeconds)
    {
        audioBytes = Array.Empty<byte>();
        contentType = GuessContentType(_options.AudioFormat);
        durationSeconds = null;

        var audioBase64 =
            TryGetString(element, "audioBase64") ??
            TryGetString(element, "audio_base64") ??
            TryGetString(element, "audio") ??
            TryGetNestedString(element, "audio", "data") ??
            TryGetNestedString(element, "audio", "base64") ??
            TryGetNestedString(element, "data", "audioBase64") ??
            TryGetNestedString(element, "data", "audio_base64") ??
            TryGetNestedString(element, "result", "audioBase64") ??
            TryGetNestedString(element, "result", "audio_base64");

        if (string.IsNullOrWhiteSpace(audioBase64))
            return false;

        try
        {
            audioBytes = Convert.FromBase64String(ExtractBase64(audioBase64));
        }
        catch (FormatException)
        {
            return false;
        }

        if (audioBytes.Length == 0)
            return false;

        contentType =
            TryGetString(element, "contentType") ??
            TryGetString(element, "content_type") ??
            TryGetNestedString(element, "audio", "format") ??
            TryGetNestedString(element, "data", "contentType") ??
            TryGetNestedString(element, "data", "content_type") ??
            GuessContentType(_options.AudioFormat);

        durationSeconds =
            TryGetInt(element, "durationSeconds") ??
            TryGetInt(element, "duration_seconds") ??
            TryGetNestedInt(element, "audio", "duration") ??
            TryGetNestedInt(element, "data", "durationSeconds") ??
            TryGetNestedInt(element, "data", "duration_seconds");

        return true;
    }

    private static bool TryGetFirstChoice(JsonElement root, out JsonElement choice)
    {
        choice = default;
        if (!root.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array)
            return false;

        foreach (var item in choices.EnumerateArray())
        {
            choice = item;
            return true;
        }

        return false;
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        value = default;
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out value);
    }

    private static bool TryGetNestedObject(JsonElement root, string propertyName, out JsonElement child)
    {
        child = default;
        if (!TryGetProperty(root, propertyName, out var value) || value.ValueKind != JsonValueKind.Object)
            return false;

        child = value;
        return true;
    }

    private async Task<TtsSynthesizeResponse?> TrySynthesizeViaStreamAsync(string baseUrl, TtsSynthesizeRequest request, CancellationToken cancellationToken)
    {
        foreach (var candidateBaseUrl in GetStreamCandidates(baseUrl))
        {
            // Try POST /stream first (supports long text and avoids URL encoding limits).
            var postResult = await TryCallStreamPostAsync(candidateBaseUrl, request, cancellationToken);
            if (postResult is not null)
            {
                _streamBaseUrlOverride = candidateBaseUrl;
                return postResult;
            }

            // Then GET /stream?text=... for server variants that only expose GET.
            var getResult = await TryCallStreamGetAsync(candidateBaseUrl, request, cancellationToken);
            if (getResult is not null)
            {
                _streamBaseUrlOverride = candidateBaseUrl;
                return getResult;
            }

            _streamProbeFailures.TryAdd(candidateBaseUrl, 0);
        }

        return null;
    }

    private IEnumerable<string> GetStreamCandidates(string configuredBaseUrl)
    {
        if (!string.IsNullOrWhiteSpace(_streamBaseUrlOverride))
        {
            yield return _streamBaseUrlOverride;
            yield break;
        }

        yield return configuredBaseUrl;

        if (!Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var uri))
            yield break;

        // Common setup: LMDeploy on 23333, FastAPI stream app on 23334 (or 8001 in some deployments).
        if (uri.Port == 23333)
        {
            var builder23334 = new UriBuilder(uri)
            {
                Port = 23334,
                Path = string.Empty,
                Query = string.Empty
            };

            var alt23334 = builder23334.Uri.GetLeftPart(UriPartial.Authority);
            if (!_streamProbeFailures.ContainsKey(alt23334))
                yield return alt23334;

            var builder8001 = new UriBuilder(uri)
            {
                Port = 8001,
                Path = string.Empty,
                Query = string.Empty
            };

            var alt8001 = builder8001.Uri.GetLeftPart(UriPartial.Authority);
            if (!_streamProbeFailures.ContainsKey(alt8001))
                yield return alt8001;
        }
    }

    private async Task<TtsSynthesizeResponse?> TryCallStreamPostAsync(string baseUrl, TtsSynthesizeRequest request, CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object?>
        {
            ["text"] = request.Text,
            ["voice_id"] = request.VoiceCode
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildUri(baseUrl, "/stream"))
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };

        if (!string.IsNullOrWhiteSpace(_options.ApiKey) && !_options.ApiKey.StartsWith("${"))
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await _http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (string.IsNullOrWhiteSpace(mediaType) || !mediaType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
            return null;

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length == 0)
            return null;

        var normalized = NormalizeWavHeaderIfNeeded(bytes, mediaType);
        var duration = EstimateDurationFromAudio(normalized, mediaType, request.Text);
        return new TtsSynthesizeResponse(normalized, mediaType, DurationSeconds: duration);
    }

    private async Task<TtsSynthesizeResponse?> TryCallStreamGetAsync(string baseUrl, TtsSynthesizeRequest request, CancellationToken cancellationToken)
    {
        var query = $"/stream?text={Uri.EscapeDataString(request.Text)}";
        if (!string.IsNullOrWhiteSpace(request.VoiceCode))
            query += $"&voice_id={Uri.EscapeDataString(request.VoiceCode)}";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, BuildUri(baseUrl, query));

        if (!string.IsNullOrWhiteSpace(_options.ApiKey) && !_options.ApiKey.StartsWith("${"))
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await _http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (string.IsNullOrWhiteSpace(mediaType) || !mediaType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
            return null;

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length == 0)
            return null;

        var normalized = NormalizeWavHeaderIfNeeded(bytes, mediaType);
        var duration = EstimateDurationFromAudio(normalized, mediaType, request.Text);
        return new TtsSynthesizeResponse(normalized, mediaType, DurationSeconds: duration);
    }

    private static byte[] NormalizeWavHeaderIfNeeded(byte[] bytes, string? mediaType)
    {
        if (bytes.Length < 44)
            return bytes;

        if (!string.Equals(mediaType, "audio/wav", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(mediaType, "audio/x-wav", StringComparison.OrdinalIgnoreCase))
            return bytes;

        if (!(bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F' &&
              bytes[8] == (byte)'W' && bytes[9] == (byte)'A' && bytes[10] == (byte)'V' && bytes[11] == (byte)'E'))
            return bytes;

        var normalized = (byte[])bytes.Clone();

        // RIFF chunk size should be fileLength - 8.
        BinaryPrimitives.WriteInt32LittleEndian(normalized.AsSpan(4, 4), normalized.Length - 8);

        // Find data chunk and set its size to remaining bytes.
        var pos = 12;
        while (pos + 8 <= normalized.Length)
        {
            var isData = normalized[pos] == (byte)'d' &&
                         normalized[pos + 1] == (byte)'a' &&
                         normalized[pos + 2] == (byte)'t' &&
                         normalized[pos + 3] == (byte)'a';

            if (isData)
            {
                var dataStart = pos + 8;
                var dataSize = Math.Max(0, normalized.Length - dataStart);
                BinaryPrimitives.WriteInt32LittleEndian(normalized.AsSpan(pos + 4, 4), dataSize);
                return normalized;
            }

            var chunkSize = BinaryPrimitives.ReadInt32LittleEndian(normalized.AsSpan(pos + 4, 4));
            if (chunkSize < 0)
                break;

            pos += 8 + chunkSize;
            if ((chunkSize & 1) == 1)
                pos += 1;
        }

        // Fallback for canonical PCM header where data size is at offset 40.
        BinaryPrimitives.WriteInt32LittleEndian(normalized.AsSpan(40, 4), Math.Max(0, normalized.Length - 44));
        return normalized;
    }

    private static string? TryGetModelHint(JsonElement root)
    {
        return TryGetString(root, "model") ??
               TryGetNestedString(root, "data", "model") ??
               TryGetNestedString(root, "result", "model");
    }

    private static bool LooksLikeBase64(string value)
    {
        if (value.Length < 24 || (value.Length % 4) != 0)
            return false;

        foreach (var ch in value)
        {
            if (!(char.IsLetterOrDigit(ch) || ch == '+' || ch == '/' || ch == '='))
                return false;
        }

        return true;
    }

    private static string BuildUri(string baseUrl, string path)
    {
        if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
            return path;

        return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }

    private static async Task<string> ReadErrorBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
            return "<empty>";

        const int maxLen = 400;
        var compact = raw.Replace("\r", " ").Replace("\n", " ").Trim();
        return compact.Length <= maxLen ? compact : compact[..maxLen];
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
        var normalized = idx >= 0 ? value[(idx + marker.Length)..] : value;

        var buffer = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (!char.IsWhiteSpace(ch))
                buffer.Append(ch);
        }

        return buffer.ToString();
    }

    private static string GuessContentType(string? configuredFormat)
    {
        if (string.Equals(configuredFormat, "wav", StringComparison.OrdinalIgnoreCase))
            return "audio/wav";

        return "audio/mpeg";
    }

    private static int EstimateDurationFromAudio(byte[] audioBytes, string? mediaType, string text)
    {
        // Try to calculate duration from WAV header first
        if (audioBytes.Length >= 44 &&
            string.Equals(mediaType, "audio/wav", StringComparison.OrdinalIgnoreCase))
        {
            // Check for valid WAV header
            if (audioBytes[0] == (byte)'R' && audioBytes[1] == (byte)'I' &&
                audioBytes[2] == (byte)'F' && audioBytes[3] == (byte)'F' &&
                audioBytes[8] == (byte)'W' && audioBytes[9] == (byte)'A' &&
                audioBytes[10] == (byte)'V' && audioBytes[11] == (byte)'E')
            {
                // Parse WAV format info at offset 22-24
                var channels = BinaryPrimitives.ReadInt16LittleEndian(audioBytes.AsSpan(22));
                var sampleRate = BinaryPrimitives.ReadInt32LittleEndian(audioBytes.AsSpan(24));
                var bitsPerSample = BinaryPrimitives.ReadInt16LittleEndian(audioBytes.AsSpan(34));

                if (sampleRate > 0 && channels > 0 && bitsPerSample > 0)
                {
                    // Find data chunk
                    var pos = 12;
                    while (pos + 8 <= audioBytes.Length)
                    {
                        var isData = audioBytes[pos] == (byte)'d' &&
                                     audioBytes[pos + 1] == (byte)'a' &&
                                     audioBytes[pos + 2] == (byte)'t' &&
                                     audioBytes[pos + 3] == (byte)'a';

                        if (isData)
                        {
                            var dataSize = BinaryPrimitives.ReadInt32LittleEndian(audioBytes.AsSpan(pos + 4, 4));
                            if (dataSize > 0)
                            {
                                var bytesPerSample = (channels * bitsPerSample) / 8;
                                var duration = (double)dataSize / (sampleRate * bytesPerSample);
                                var result = (int)Math.Ceiling(duration);
                                if (result > 0) return result;
                            }
                            break;
                        }

                        var chunkSize = BinaryPrimitives.ReadInt32LittleEndian(audioBytes.AsSpan(pos + 4, 4));
                        if (chunkSize < 0)
                            break;

                        pos += 8 + chunkSize;
                        if ((chunkSize & 1) == 1)
                            pos += 1;
                    }
                }
            }
        }

        // Fallback: estimate duration from text length
        // Vietnamese average: ~4-5 characters per second at normal speed
        var nonWhitespaceChars = 0;
        foreach (var ch in text)
        {
            if (!char.IsWhiteSpace(ch))
                nonWhitespaceChars++;
        }

        if (nonWhitespaceChars > 0)
        {
            var estimatedSeconds = (int)Math.Ceiling(nonWhitespaceChars / 4.0);
            return Math.Max(1, estimatedSeconds);
        }

        // Last resort: return 1 second minimum if we have audio bytes
        // This ensures we never return 0 or null duration
        return Math.Max(1, audioBytes.Length / 1000);
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

