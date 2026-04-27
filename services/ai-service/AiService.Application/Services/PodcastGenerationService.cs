using AiService.Application.Enums;
using AiService.Application.Interfaces;
using AiService.Application.Results;

namespace AiService.Application.Services;

public class PodcastGenerationService : IPodcastGenerationService
{
    private readonly IGeminiService _geminiService;
    private readonly ITextToSpeechService _textToSpeechService;

    public PodcastGenerationService(
        IGeminiService geminiService,
        ITextToSpeechService textToSpeechService)
    {
        _geminiService = geminiService;
        _textToSpeechService = textToSpeechService;
    }

    public async Task<Result<PodcastGenerateResult>> GeneratePodcastAudioAsync(
        PodcastGenerateRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Topic))
        {
            return Result<PodcastGenerateResult>.Failure("topic is required", (int)ApiStatusCode.HB40001);
        }

        if (string.IsNullOrWhiteSpace(request.Voice))
        {
            return Result<PodcastGenerateResult>.Failure("voice is required", (int)ApiStatusCode.HB40001);
        }

        Result<string> scriptResult;
        try
        {
            scriptResult = await _geminiService.GeneratePodcastScriptAsync(
                new GeneratePodcastScriptFlowRequest(
                    Topic: request.Topic,
                    Style: request.Style,
                    Duration: request.Duration,
                    Language: request.Language,
                    ModelName: request.ModelName),
                cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<PodcastGenerateResult>.Failure($"Gemini generation failed: {ex.Message}", (int)ApiStatusCode.HB50001);
        }

        if (!scriptResult.IsSuccess || string.IsNullOrWhiteSpace(scriptResult.Data))
        {
            return Result<PodcastGenerateResult>.Failure(
                scriptResult.ErrorMessage ?? "Gemini generation failed",
                scriptResult.ErrorCode ?? (int)ApiStatusCode.HB50001);
        }

        Result<TextToSpeechResult> ttsResult;
        try
        {
            ttsResult = await _textToSpeechService.GenerateAudioAsync(
                new TextToSpeechRequest(
                    Text: scriptResult.Data,
                    Voice: request.Voice,
                    IncludeAudioBytes: request.IncludeAudioBytes),
                cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<PodcastGenerateResult>.Failure($"TTS generation failed: {ex.Message}", (int)ApiStatusCode.HB50001);
        }

        if (!ttsResult.IsSuccess || ttsResult.Data is null)
        {
            return Result<PodcastGenerateResult>.Failure(
                ttsResult.ErrorMessage ?? "TTS generation failed",
                ttsResult.ErrorCode ?? (int)ApiStatusCode.HB50001);
        }

        var payload = new PodcastGenerateResult(
            Script: scriptResult.Data,
            AudioUrl: ttsResult.Data.AudioUrl,
            AudioBytes: ttsResult.Data.AudioBytes,
            Duration: ttsResult.Data.Duration,
            Provider: ttsResult.Data.Provider);

        return Result<PodcastGenerateResult>.Success(payload);
    }
}
