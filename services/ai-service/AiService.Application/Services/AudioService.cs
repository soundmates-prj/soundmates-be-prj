using AiService.Application.Constants;
using AiService.Application.Interfaces;
using AiService.Application.Results;
using AiService.Application.Enums;
using AiService.Domain.Entities;
using AiService.Domain.Enums;
using AiService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;

namespace AiService.Application.Services;

public class AudioService : IAudioService
{
    private readonly IScriptRepository _scripts;
    private readonly IVoiceRepository _voices;
    private readonly IScriptAudioRepository _audios;
    private readonly IUnitOfWork _uow;
    private readonly ITtsClient _tts;
    private readonly IAudioStorage _storage;
    private readonly IAudioConversionService _audioConversion;
    private readonly IUsageService _usage;
    private readonly ILogger<AudioService> _logger;

    public AudioService(
        IScriptRepository scripts,
        IVoiceRepository voices,
        IScriptAudioRepository audios,
        IUnitOfWork uow,
        ITtsClient tts,
        IAudioStorage storage,
        IAudioConversionService audioConversion,
        IUsageService usage,
        ILogger<AudioService> logger)
    {
        _scripts = scripts;
        _voices = voices;
        _audios = audios;
        _uow = uow;
        _tts = tts;
        _storage = storage;
        _audioConversion = audioConversion;
        _usage = usage;
        _logger = logger;
    }

    public async Task<Result<ScriptAudio>> GenerateAsync(GenerateAudioFromScriptRequest request, CancellationToken cancellationToken)
    {
        var script = await _scripts.GetByIdAsync(request.ScriptId, cancellationToken);
        if (script is null)
            return Result<ScriptAudio>.Failure("script not found");
        if (script.AuthorId != request.UserId)
            throw new UnauthorizedAccessException();

        var voice = await _voices.GetByCodeAsync(AiProviderConstants.VieNeuTts, request.VoiceCode, cancellationToken);
        if (voice is null)
            return Result<ScriptAudio>.Failure($"Voice '{request.VoiceCode}' not found");

        var audio = new ScriptAudio
        {
            AudioId = Guid.NewGuid(),
            ScriptId = script.ScriptId,
            VoiceId = voice.VoiceId,
            Speed = request.Speed,
            Pitch = request.Pitch,
            Duration = null,
            Status = AudioStatus.Processing.ToString().ToLowerInvariant(),
            CreatedAt = DateTime.UtcNow,
            AudioUrl = "",
            AudioPath = ""
        };

        await _audios.AddAsync(audio, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        try
        {
            // --- TEXT CHUNKING LOGIC ---
            var textChunks = SplitTextToChunks(script.ContentText, maxChunkLength: 300);
            _logger.LogInformation("Split text into {Count} chunks for TTS generation", textChunks.Count);

            var audioChunks = new List<byte[]>();
            var totalDuration = 0;
            string? firstContentType = null;

            foreach (var chunk in textChunks)
            {
                if (string.IsNullOrWhiteSpace(chunk)) continue;

                var ttsResp = await _tts.SynthesizeAsync(
                    new TtsSynthesizeRequest(
                        Text: chunk,
                        VoiceCode: voice.VoiceCode,
                        Model: voice.Model,
                        Speed: request.Speed,
                        Pitch: request.Pitch),
                    cancellationToken);

                var validationError = ValidateTtsResponse(chunk, (double)(request.Speed ?? 1.0m), ttsResp);
                if (validationError is not null)
                {
                    _logger.LogError("TTS Chunk validation failed: {Error}", validationError);
                    audio.Status = AudioStatus.Failed.ToString().ToLowerInvariant();
                    audio.UpdatedAt = DateTime.UtcNow;
                    await _audios.UpdateAsync(audio, cancellationToken);
                    await _uow.SaveChangesAsync(cancellationToken);
                    return Result<ScriptAudio>.Failure($"Chunk generation failed: {validationError}", (int)ApiStatusCode.HB50001);
                }

                audioChunks.Add(ttsResp.AudioBytes);
                totalDuration += ttsResp.DurationSeconds ?? 0;
                firstContentType ??= ttsResp.ContentType;
            }

            if (audioChunks.Count == 0)
                return Result<ScriptAudio>.Failure("No audio generated");

            var ext = (firstContentType ?? "").Contains("wav", StringComparison.OrdinalIgnoreCase) ? ".wav" : ".mp3";
            
            // Concatenate all chunks
            var concatResult = await _audioConversion.ConcatenateAudiosAsync(audioChunks, ext, cancellationToken);
            if (!concatResult.IsSuccess || concatResult.ConcatenatedBytes == null)
            {
                return Result<ScriptAudio>.Failure($"Audio concatenation failed: {concatResult.ErrorMessage}", (int)ApiStatusCode.HB50001);
            }

            var finalBytes = concatResult.ConcatenatedBytes;
            var contentType = firstContentType ?? "audio/mpeg";

            if (!string.IsNullOrWhiteSpace(request.BgmUrl))
            {
                var mixResult = await _audioConversion.MixWithBackgroundMusicAsync(
                    finalBytes, 
                    ext, 
                    request.BgmUrl, 
                    cancellationToken);
                
                if (mixResult.IsSuccess && mixResult.MixedBytes != null)
                {
                    finalBytes = mixResult.MixedBytes;
                    ext = ".mp3";
                    contentType = "audio/mpeg";
                }
            }

            var stored = await _storage.SaveAsync(
                fileNameWithoutExtension: audio.AudioId.ToString("N"),
                extensionWithDot: ext,
                contentType: contentType,
                bytes: finalBytes,
                cancellationToken: cancellationToken);

            audio.AudioPath = stored.RelativePath;
            audio.AudioUrl = $"/api/audios/{audio.AudioId}/file";
            audio.Duration = totalDuration;
            audio.Status = AudioStatus.Done.ToString().ToLowerInvariant();
            audio.UpdatedAt = DateTime.UtcNow;

            await _audios.UpdateAsync(audio, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return Result<ScriptAudio>.Success(audio);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audio generation failed");
            audio.Status = AudioStatus.Failed.ToString().ToLowerInvariant();
            audio.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _audios.UpdateAsync(audio, CancellationToken.None);
                await _uow.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception persistEx)
            {
                _logger.LogWarning(persistEx, "Failed to persist audio failure status");
            }

            throw;
        }
    }

    private List<string> SplitTextToChunks(string text, int maxChunkLength)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();
        if (text.Length <= maxChunkLength) return new List<string> { text };

        var chunks = new List<string>();
        var sentences = text.Split(new[] { ". ", "! ", "? ", ".\n", "!\n", "?\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        var currentChunk = new StringBuilder();
        foreach (var sentence in sentences)
        {
            var trimmed = sentence.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            // Re-add the punctuation if it's not the end of the text
            var sentenceWithPunct = trimmed;
            if (!char.IsPunctuation(trimmed.Last())) sentenceWithPunct += ".";

            if (currentChunk.Length + sentenceWithPunct.Length > maxChunkLength && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());
                currentChunk.Clear();
            }
            
            currentChunk.Append(sentenceWithPunct).Append(" ");
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }

    private static string? ValidateTtsResponse(string sourceText, double speed, TtsSynthesizeResponse response)
    {
        if (response.AudioBytes.Length == 0)
            return "TTS returned empty audio bytes.";

        if (!response.ContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
            return $"TTS returned invalid content type '{response.ContentType}'.";

        var isWav = response.ContentType.Contains("wav", StringComparison.OrdinalIgnoreCase);
        var minLength = isWav ? 44 : 128; // Reduced min size for chunks
        if (response.AudioBytes.Length < minLength)
            return $"TTS returned suspiciously small audio payload ({response.AudioBytes.Length} bytes).";

        if (isWav)
        {
            if (!(response.AudioBytes[0] == 'R' && response.AudioBytes[1] == 'I' && response.AudioBytes[2] == 'F' && response.AudioBytes[3] == 'F'))
                return "TTS WAV payload is missing RIFF header.";
        }

        return null;
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

    public async Task<Result<ScriptAudio>> GetByIdAsync(Guid audioId, CancellationToken cancellationToken)
    {
        var audio = await _audios.GetByIdAsync(audioId, cancellationToken);
        return audio is null
            ? Result<ScriptAudio>.Failure("audio not found")
            : Result<ScriptAudio>.Success(audio);
    }

    public async Task<Result<AudioFileStreamResult>> OpenReadForUserAsync(Guid userId, Guid audioId, CancellationToken cancellationToken)
    {
        var audio = await _audios.GetByIdAsync(audioId, cancellationToken);
        if (audio is null)
            return Result<AudioFileStreamResult>.Failure("audio not found", (int)ApiStatusCode.HB40401);

        if (audio.Script.AuthorId != userId)
            return Result<AudioFileStreamResult>.Failure("forbidden", (int)ApiStatusCode.HB40301);

        var (stream, contentType, contentLength) = await _storage.OpenReadAsync(audio.AudioPath, cancellationToken);
        return Result<AudioFileStreamResult>.Success(new AudioFileStreamResult(stream, contentType, contentLength));
    }

    public async Task<Result<AudioFileStreamResult>> OpenReadAnonymousAsync(Guid audioId, CancellationToken cancellationToken)
    {
        var audio = await _audios.GetByIdAsync(audioId, cancellationToken);
        if (audio is null)
            return Result<AudioFileStreamResult>.Failure("audio not found", (int)ApiStatusCode.HB40401);

        var (stream, contentType, contentLength) = await _storage.OpenReadAsync(audio.AudioPath, cancellationToken);
        return Result<AudioFileStreamResult>.Success(new AudioFileStreamResult(stream, contentType, contentLength));
    }

    public async Task<Result<IReadOnlyList<ScriptAudio>>> ListForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var audios = await _audios.GetForUserAsync(userId, cancellationToken);
        return Result<IReadOnlyList<ScriptAudio>>.Success(audios);
    }

    public async Task<Result<bool>> DeleteAsync(Guid userId, Guid audioId, CancellationToken cancellationToken)
    {
        var audio = await _audios.GetByIdAsync(audioId, cancellationToken);
        if (audio is null)
            return Result<bool>.Failure("audio not found", (int)ApiStatusCode.HB40401);

        if (audio.Script.AuthorId != userId)
            return Result<bool>.Failure("forbidden", (int)ApiStatusCode.HB40301);

        // Delete from storage
        if (!string.IsNullOrWhiteSpace(audio.AudioPath))
        {
            try { await _storage.DeleteAsync(audio.AudioPath, cancellationToken); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete audio file {Path}", audio.AudioPath); }
        }

        // Delete from DB
        await _audios.DeleteAsync(audioId, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}

