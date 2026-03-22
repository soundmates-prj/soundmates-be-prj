using AiService.Application.Constants;
using AiService.Application.Enums;
using AiService.Application.Interfaces;
using AiService.Application.Results;

namespace AiService.Infrastructure.Services;

public class VieneuTextToSpeechService : ITextToSpeechService
{
    private readonly ITtsClient _ttsClient;
    private readonly IAudioStorage _audioStorage;

    public VieneuTextToSpeechService(ITtsClient ttsClient, IAudioStorage audioStorage)
    {
        _ttsClient = ttsClient;
        _audioStorage = audioStorage;
    }

    public async Task<Result<TextToSpeechResult>> GenerateAudioAsync(TextToSpeechRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return Result<TextToSpeechResult>.Failure("text is required", (int)ApiStatusCode.HB40001);
        }

        if (string.IsNullOrWhiteSpace(request.Voice))
        {
            return Result<TextToSpeechResult>.Failure("voice is required", (int)ApiStatusCode.HB40001);
        }

        var ttsResponse = await _ttsClient.SynthesizeAsync(
            new TtsSynthesizeRequest(
                Text: request.Text,
                VoiceCode: request.Voice,
                Model: null,
                Speed: null,
                Pitch: null),
            cancellationToken);

        var validation = ValidateAudioResponse(request.Text, ttsResponse);
        if (!validation.IsSuccess)
        {
            return Result<TextToSpeechResult>.Failure(validation.ErrorMessage ?? "TTS returned invalid audio", (int)ApiStatusCode.HB50001);
        }

        // Luu file de co metadata/duong dan tai su dung noi bo khi can.
        var extension = ttsResponse.ContentType.Contains("wav", StringComparison.OrdinalIgnoreCase) ? ".wav" : ".mp3";
        var stored = await _audioStorage.SaveAsync(
            fileNameWithoutExtension: Guid.NewGuid().ToString("N"),
            extensionWithDot: extension,
            contentType: ttsResponse.ContentType,
            bytes: ttsResponse.AudioBytes,
            cancellationToken: cancellationToken);

        var result = new TextToSpeechResult(
            AudioUrl: stored.RelativePath,
            AudioBytes: request.IncludeAudioBytes ? ttsResponse.AudioBytes : null,
            Duration: ttsResponse.DurationSeconds,
            ContentType: ttsResponse.ContentType,
            Provider: AiProviderConstants.VieNeuTts);

        return Result<TextToSpeechResult>.Success(result);
    }

    private static Result<bool> ValidateAudioResponse(string sourceText, TtsSynthesizeResponse response)
    {
        if (response.AudioBytes.Length == 0)
            return Result<bool>.Failure("TTS returned empty audio", (int)ApiStatusCode.HB50001);

        if (!response.ContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
            return Result<bool>.Failure($"TTS returned invalid content type '{response.ContentType}'", (int)ApiStatusCode.HB50001);

        var isWav = response.ContentType.Contains("wav", StringComparison.OrdinalIgnoreCase);
        var minBytes = isWav ? 45 : 256;
        if (response.AudioBytes.Length < minBytes)
            return Result<bool>.Failure($"TTS returned suspiciously small audio payload ({response.AudioBytes.Length} bytes)", (int)ApiStatusCode.HB50001);

        if (isWav)
        {
            var wavValidation = ValidateWavHeader(response.AudioBytes);
            if (!wavValidation.IsSuccess)
                return wavValidation;
        }

        // Duration must be present and positive
        if (!response.DurationSeconds.HasValue || response.DurationSeconds.Value <= 0)
            return Result<bool>.Failure($"TTS returned invalid duration ({(response.DurationSeconds.HasValue ? response.DurationSeconds.Value : 0)}s)", (int)ApiStatusCode.HB50001);

        var compactLength = CountNonWhitespaceChars(sourceText);
        if (compactLength >= 40)
        {
            var minimumDuration = (int)Math.Ceiling(compactLength / 35d);
            if (response.DurationSeconds.Value < minimumDuration)
                return Result<bool>.Failure(
                    $"TTS returned suspiciously short duration ({response.DurationSeconds.Value}s for {compactLength} chars)",
                    (int)ApiStatusCode.HB50001);
        }

        return Result<bool>.Success(true);
    }

    private static Result<bool> ValidateWavHeader(byte[] bytes)
    {
        if (bytes.Length < 44)
            return Result<bool>.Failure("TTS WAV payload is too small", (int)ApiStatusCode.HB50001);

        if (!(bytes[0] == 'R' && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == 'F'))
            return Result<bool>.Failure("TTS WAV payload is missing RIFF header", (int)ApiStatusCode.HB50001);

        if (!(bytes[8] == 'W' && bytes[9] == 'A' && bytes[10] == 'V' && bytes[11] == 'E'))
            return Result<bool>.Failure("TTS WAV payload is missing WAVE header", (int)ApiStatusCode.HB50001);

        var dataChunkSize = FindWavDataChunkSize(bytes);
        if (dataChunkSize <= 0)
            return Result<bool>.Failure("TTS WAV payload does not contain audio frames", (int)ApiStatusCode.HB50001);

        return Result<bool>.Success(true);
    }

    private static int FindWavDataChunkSize(byte[] bytes)
    {
        var offset = 12;
        while (offset + 8 <= bytes.Length)
        {
            var isDataChunk =
                bytes[offset] == 'd' &&
                bytes[offset + 1] == 'a' &&
                bytes[offset + 2] == 't' &&
                bytes[offset + 3] == 'a';

            var chunkSize = BitConverter.ToInt32(bytes, offset + 4);
            if (isDataChunk)
                return chunkSize;

            if (chunkSize < 0)
                return -1;

            var paddedChunkSize = chunkSize + (chunkSize % 2);
            offset += 8 + paddedChunkSize;
        }

        return -1;
    }

    private static int CountNonWhitespaceChars(string text)
    {
        var count = 0;
        foreach (var ch in text)
        {
            if (!char.IsWhiteSpace(ch))
                count++;
        }

        return count;
    }
}
