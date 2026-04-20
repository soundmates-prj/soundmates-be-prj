using AiService.Application.Constants;
using AiService.Application.Enums;
using AiService.Application.Interfaces;
using AiService.Application.Results;
using AiService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace AiService.Infrastructure.Services;

public class VieneuTextToSpeechService : ITextToSpeechService
{
    private readonly ITtsClient _ttsClient;
    private readonly IAudioStorage _localStorage;
    private readonly ICloudinaryAudioStorage _cloudinaryStorage;
    private readonly IAudioConversionService _audioConversion;
    private readonly IVoiceRepository _voiceRepository;
    private readonly Microsoft.Extensions.Logging.ILogger<VieneuTextToSpeechService> _logger;

    public VieneuTextToSpeechService(
        ITtsClient ttsClient,
        IAudioStorage localStorage,
        ICloudinaryAudioStorage cloudinaryStorage,
        IAudioConversionService audioConversion,
        IVoiceRepository voiceRepository,
        Microsoft.Extensions.Logging.ILogger<VieneuTextToSpeechService> logger)
    {
        _ttsClient = ttsClient;
        _localStorage = localStorage;
        _cloudinaryStorage = cloudinaryStorage;
        _audioConversion = audioConversion;
        _voiceRepository = voiceRepository;
        _logger = logger;
    }

    public async Task<Result<TextToSpeechResult>> GenerateAudioAsync(TextToSpeechRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return Result<TextToSpeechResult>.Failure("text is required", (int)ApiStatusCode.HB40001);
        }

        // --- Log incoming voice request ---
        var voiceCodeToUse = request.Voice;
        _logger.LogInformation(
            "[TTS] Voice lookup request: voiceCode='{VoiceCode}', provider='{Provider}'",
            voiceCodeToUse, AiProviderConstants.VieNeuTts);

        var voice = await _voiceRepository.GetByCodeAsync(AiProviderConstants.VieNeuTts, voiceCodeToUse, cancellationToken);

        _logger.LogInformation(
            "[TTS] DB lookup result: voice={VoiceFound}, voiceCodeUsed='{VoiceCodeUsed}', model='{Model}'",
            voice?.DisplayName ?? "(null)", voiceCodeToUse, voice?.Model ?? "(null)");

        if (voice == null)
        {
            // Fallback to q4 baseline
            voiceCodeToUse = "q4";
            _logger.LogWarning("[TTS] Cloned voice not found in DB, falling back to 'q4'");
            voice = await _voiceRepository.GetByCodeAsync(AiProviderConstants.VieNeuTts, voiceCodeToUse, cancellationToken);

            // If still null, try to take the first active one
            if (voice == null)
            {
                var activeVoices = await _voiceRepository.GetActiveAsync(cancellationToken);
                voice = activeVoices.FirstOrDefault();
                if (voice != null)
                {
                    voiceCodeToUse = voice.VoiceCode;
                    _logger.LogWarning("[TTS] Fallback 'q4' not found either. Using first active voice: '{Name}' ({Code})",
                        voice.DisplayName, voice.VoiceCode);
                }
            }
        }

        _logger.LogInformation(
            "[TTS] Final TTS request: voiceCode='{FinalVoiceCode}', model='{FinalModel}'",
            voiceCodeToUse, voice?.Model ?? "(null)");

        if (voice == null)
        {
            return Result<TextToSpeechResult>.Failure("No active TTS voices available in the system", (int)ApiStatusCode.HB50001);
        }

        var ttsResponse = await _ttsClient.SynthesizeAsync(
            new TtsSynthesizeRequest(
                Text: request.Text,
                VoiceCode: voiceCodeToUse,
                Model: voice.Model,
                Speed: null,
                Pitch: null),
            cancellationToken);

        var validation = ValidateAudioResponse(request.Text, ttsResponse);
        if (!validation.IsSuccess)
        {
            return Result<TextToSpeechResult>.Failure(validation.ErrorMessage ?? "TTS returned invalid audio", (int)ApiStatusCode.HB50001);
        }

        // Step 1: Determine input extension from content type
        var rawExtension = ttsResponse.ContentType.Contains("wav", StringComparison.OrdinalIgnoreCase) ? ".wav" : ".mp3";

        // Step 2: Convert WAV → MP3
        var conversionResult = await _audioConversion.ConvertToMp3Async(
            ttsResponse.AudioBytes,
            rawExtension,
            cancellationToken);

        if (!conversionResult.IsSuccess || conversionResult.Mp3Bytes == null)
        {
            return Result<TextToSpeechResult>.Failure(
                $"Audio conversion failed: {conversionResult.ErrorMessage}",
                (int)ApiStatusCode.HB50001);
        }

        var audioBytes = conversionResult.Mp3Bytes;
        var mp3FileName = $"{Guid.NewGuid():N}.mp3";

        // Step 3: Upload MP3 to Cloudinary
        string audioUrl;
        try
        {
            var uploadResult = await _cloudinaryStorage.UploadAudioAsync(
                audioBytes,
                mp3FileName,
                "audio/mpeg",
                cancellationToken);
            audioUrl = uploadResult.Url;
            _logger.LogInformation(
                "Podcast audio uploaded to Cloudinary: {Url}",
                audioUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cloudinary upload failed, falling back to local storage");
            // Fallback to local storage if Cloudinary fails
            var stored = await _localStorage.SaveAsync(
                fileNameWithoutExtension: Guid.NewGuid().ToString("N"),
                extensionWithDot: ".mp3",
                contentType: "audio/mpeg",
                bytes: audioBytes,
                cancellationToken: cancellationToken);
            audioUrl = stored.RelativePath;
        }

        var result = new TextToSpeechResult(
            AudioUrl: audioUrl,
            AudioBytes: request.IncludeAudioBytes ? audioBytes : null,
            Duration: ttsResponse.DurationSeconds,
            ContentType: "audio/mpeg",
            Provider: AiProviderConstants.VieNeuTts);

        return Result<TextToSpeechResult>.Success(result);
    }

    public async Task<Result<bool>> CloneVoiceAsync(string voiceId, string refText, byte[] audioBytes, string fileName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(voiceId))
            return Result<bool>.Failure("voiceId is required", (int)ApiStatusCode.HB40001);

        if (string.IsNullOrWhiteSpace(refText))
            return Result<bool>.Failure("refText is required", (int)ApiStatusCode.HB40001);

        if (audioBytes == null || audioBytes.Length == 0)
            return Result<bool>.Failure("audio file is empty", (int)ApiStatusCode.HB40001);

        try
        {
            var success = await _ttsClient.CloneVoiceAsync(voiceId, refText, audioBytes, fileName, cancellationToken);
            return success
                ? Result<bool>.Success(true)
                : Result<bool>.Failure("TTS server failed to clone voice", (int)ApiStatusCode.HB50001);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(ex.Message, (int)ApiStatusCode.HB40001);
        }
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
        if (compactLength >= 20)
        {
            // Note: Keep in sync with AudioService.cs
            var minimumDuration = (int)Math.Floor(compactLength / 35d);
            if (minimumDuration > 2 && response.DurationSeconds.Value < minimumDuration)
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
            {
                // If chunk size is 0 (placeholder from Python wave.open),
                // fall back to actual remaining bytes after the chunk header
                if (chunkSize > 0)
                    return chunkSize;
                var actualDataSize = bytes.Length - (offset + 8);
                return actualDataSize > 0 ? actualDataSize : -1;
            }

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
