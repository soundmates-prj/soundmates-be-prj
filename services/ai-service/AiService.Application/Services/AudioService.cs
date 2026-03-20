using AiService.Application.Interfaces;
using AiService.Application.Results;
using AiService.Application.Enums;
using AiService.Domain.Entities;
using AiService.Domain.Enums;
using AiService.Domain.Interfaces;

namespace AiService.Application.Services;

public class AudioService : IAudioService
{
    private readonly IScriptRepository _scripts;
    private readonly IVoiceRepository _voices;
    private readonly IScriptAudioRepository _audios;
    private readonly IUnitOfWork _uow;
    private readonly ITtsClient _tts;
    private readonly IAudioStorage _storage;
    private readonly IUsageService _usage;

    public AudioService(
        IScriptRepository scripts,
        IVoiceRepository voices,
        IScriptAudioRepository audios,
        IUnitOfWork uow,
        ITtsClient tts,
        IAudioStorage storage,
        IUsageService usage)
    {
        _scripts = scripts;
        _voices = voices;
        _audios = audios;
        _uow = uow;
        _tts = tts;
        _storage = storage;
        _usage = usage;
    }

    public async Task<Result<ScriptAudio>> GenerateAsync(GenerateAudioFromScriptRequest request, CancellationToken cancellationToken)
    {
        var script = await _scripts.GetByIdAsync(request.ScriptId, cancellationToken);
        if (script is null)
            return Result<ScriptAudio>.Failure("script not found");
        if (script.AuthorId != request.UserId)
            throw new UnauthorizedAccessException();

        var voice = await _voices.GetByIdAsync(request.VoiceId, cancellationToken);
        if (voice is null)
            return Result<ScriptAudio>.Failure("voice not found");

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
            var ttsResp = await _tts.SynthesizeAsync(
                new TtsSynthesizeRequest(
                    Text: script.ContentText,
                    VoiceCode: voice.VoiceCode,
                    Model: voice.Model,
                    Speed: request.Speed,
                    Pitch: request.Pitch),
                cancellationToken);

            var ext = ttsResp.ContentType.Contains("wav", StringComparison.OrdinalIgnoreCase) ? ".wav" : ".mp3";
            var stored = await _storage.SaveAsync(
                fileNameWithoutExtension: audio.AudioId.ToString("N"),
                extensionWithDot: ext,
                contentType: ttsResp.ContentType,
                bytes: ttsResp.AudioBytes,
                cancellationToken: cancellationToken);

            audio.AudioPath = stored.RelativePath;
            audio.AudioUrl = $"/api/audios/{audio.AudioId}/file";
            audio.Duration = ttsResp.DurationSeconds;
            audio.Status = AudioStatus.Done.ToString().ToLowerInvariant();
            audio.UpdatedAt = DateTime.UtcNow;

            await _audios.UpdateAsync(audio, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            if (ttsResp.TokensUsed is not null || ttsResp.Cost is not null)
            {
                await _usage.LogAsync(new AiUsage
                {
                    UsageId = Guid.NewGuid(),
                    UserId = request.UserId,
                    Provider = "tts",
                    TokensUsed = ttsResp.TokensUsed,
                    Cost = ttsResp.Cost,
                    ScriptId = script.ScriptId,
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
            }

            return Result<ScriptAudio>.Success(audio);
        }
        catch
        {
            audio.Status = AudioStatus.Failed.ToString().ToLowerInvariant();
            audio.UpdatedAt = DateTime.UtcNow;
            await _audios.UpdateAsync(audio, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
            throw;
        }
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
}

