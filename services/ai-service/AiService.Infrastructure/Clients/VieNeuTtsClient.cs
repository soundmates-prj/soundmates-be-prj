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

        // This container may be either:
        // - a custom TTS endpoint returning audio bytes/base64 (legacy /v1/tts/synthesize style)
        // - a LMDeploy/FastAPI server exposing OpenAI-like chat endpoints (e.g. /v1/chat/completions)
        // We support both; if the server returns non-decodable "audio" (speech tokens),
        // we can fall back to silent WAV in demo mode.
        if (string.Equals(synthesizePath, "/v1/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            return await SynthesizeViaChatCompletionsAsync(request, cancellationToken);
        }

        // Legacy: POST {baseUrl}{synthesizePath} with {text, voice, speed, pitch, model, format, prompt}
        var payload = new Dictionary<string, object?>
        {
            ["text"] = request.Text,
            ["voice"] = request.VoiceCode,
            ["speed"] = request.Speed,
            ["pitch"] = request.Pitch,
            // Prefer per-voice model stored in DB; fallback to global config.
            ["model"] = string.IsNullOrWhiteSpace(request.Model) || request.Model.StartsWith("${")
                ? _options.Model
                : request.Model,
            ["format"] = string.IsNullOrWhiteSpace(_options.AudioFormat) ? "mp3" : _options.AudioFormat,
            ["prompt"] = BuildPrompt(request)
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildUri(_options.BaseUrl, synthesizePath))
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };

        if (!string.IsNullOrWhiteSpace(_options.ApiKey) && !_options.ApiKey.StartsWith("${"))
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

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

    private async Task<TtsSynthesizeResponse> SynthesizeViaChatCompletionsAsync(TtsSynthesizeRequest request, CancellationToken cancellationToken)
    {
        var ttsModel = string.IsNullOrWhiteSpace(request.Model) || request.Model.StartsWith("${")
            ? _options.Model
            : request.Model;

        // Build a chat prompt; some servers may ignore it but it won't break.
        // If response_format is supported, request JSON output with audioBase64.
        var messageContent = BuildChatPrompt(request);

        var payload = new
        {
            model = ttsModel,
            messages = new[]
            {
                new { role = "user", content = messageContent }
            },
            max_tokens = 2048,
            temperature = 1.0,
            top_k = 50,
            stop = new[] { "<|SPEECH_GENERATION_END|>" },
            stream = false
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildUri(_options.BaseUrl, "/v1/chat/completions"))
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };

        if (!string.IsNullOrWhiteSpace(_options.ApiKey) && !_options.ApiKey.StartsWith("${"))
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await _http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(raw);

        // Extract choices[0].message.content
        var content =
            document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

        if (string.IsNullOrWhiteSpace(content))
            return DemoFallback(raw);

        // content can be:
        // - a JSON string (because we requested json_schema)
        // - plain text
        try
        {
            using var contentDoc = JsonDocument.Parse(content);
            if (contentDoc.RootElement.TryGetProperty("audioBase64", out var audioBase64El))
            {
                var audioBase64 = audioBase64El.GetString();
                if (!string.IsNullOrWhiteSpace(audioBase64))
                {
                    try
                    {
                        var normalizedBase64 = ExtractBase64(audioBase64);
                        var audioBytes = Convert.FromBase64String(normalizedBase64);
                        return new TtsSynthesizeResponse(
                            audioBytes,
                            "audio/wav",
                            DurationSeconds: 1,
                            RawProviderResponse: raw);
                    }
                    catch (FormatException)
                    {
                        // Often the server returns speech tokens like <|speech_...|> instead of base64.
                        return DemoFallback(raw);
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Content isn't JSON; can't extract audio.
        }

        return DemoFallback(raw);
    }

    private string BuildChatPrompt(TtsSynthesizeRequest request)
    {
        // If PromptTemplate exists, reuse it as prompt for the chat model.
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

    private TtsSynthesizeResponse DemoFallback(string rawProviderResponse)
    {
        if (!_options.DemoMode)
            throw new InvalidOperationException("VieNeuTTS did not return decodable audio; enable TTS_DEMO_MODE to use a silent audio fallback.");

        // Check desired format
        var format = string.IsNullOrWhiteSpace(_options.AudioFormat) ? "mp3" : _options.AudioFormat.ToLowerInvariant();
        
        if (format == "mp3")
        {
            // 1 second silent MP3 (minimal valid MP3 frame)
            var mp3Bytes = BuildSilentMp3Bytes();
            return new TtsSynthesizeResponse(
                AudioBytes: mp3Bytes,
                ContentType: "audio/mpeg",
                DurationSeconds: 1,
                RawProviderResponse: rawProviderResponse);
        }
        else
        {
            // 1 second silent WAV.
            var wavBytes = BuildSilentWavBytes(seconds: 1, sampleRate: 16000, channels: 1);
            return new TtsSynthesizeResponse(
                AudioBytes: wavBytes,
                ContentType: "audio/wav",
                DurationSeconds: 1,
                RawProviderResponse: rawProviderResponse);
        }
    }

    private static byte[] BuildSilentWavBytes(int seconds, int sampleRate, int channels)
    {
        // PCM 16-bit little-endian silence.
        const short bitsPerSample = 16;
        int numSamples = checked(sampleRate * seconds);
        int blockAlign = channels * (bitsPerSample / 8);
        int byteRate = sampleRate * blockAlign;
        int dataSize = numSamples * blockAlign;
        int chunkSize = 36 + dataSize;

        using var ms = new System.IO.MemoryStream(44 + dataSize);
        using var bw = new System.IO.BinaryWriter(ms);

        // RIFF header
        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(chunkSize);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

        // fmt subchunk
        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16); // PCM
        bw.Write((short)1); // AudioFormat = PCM
        bw.Write((short)channels);
        bw.Write(sampleRate);
        bw.Write(byteRate);
        bw.Write((short)blockAlign);
        bw.Write(bitsPerSample);

        // data subchunk
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        bw.Write(dataSize);

        // Write silence frames
        for (int i = 0; i < numSamples * channels; i++)
            bw.Write((short)0);

        bw.Flush();
        return ms.ToArray();
    }

    private static byte[] BuildSilentMp3Bytes()
    {
        // Minimal valid MP3 frame with silence (1 second at 44.1kHz)
        // This is a very basic MP3 frame - in production you'd use a proper MP3 encoder
        return new byte[] {
            0xFF, 0xFB, 0x90, 0x00, // MP3 header
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Silent data
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };
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

