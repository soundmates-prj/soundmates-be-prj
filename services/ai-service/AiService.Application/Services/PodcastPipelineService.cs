using AiService.Application.Interfaces;
using AiService.Application.Results;
using AiService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AiService.Application.Services;

public class PodcastPipelineService : IPodcastPipelineService
{
    private readonly IScriptService _scriptService;
    private readonly IAudioService _audioService;
    private readonly IPodcastSyncClient _syncClient;
    private readonly ILogger<PodcastPipelineService> _logger;

    public PodcastPipelineService(
        IScriptService scriptService, 
        IAudioService audioService, 
        IPodcastSyncClient syncClient, 
        ILogger<PodcastPipelineService> logger)
    {
        _scriptService = scriptService;
        _audioService = audioService;
        _syncClient = syncClient;
        _logger = logger;
    }

    public async Task<Result<PodcastExecutionResult>> GenerateAndSyncPodcastAsync(GeneratePodcastRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Starting podcast pipeline for topic {Topic}", request.Topic);
        
        // 1. Generate Script using Gemini (via ScriptService)
        var scriptResult = await _scriptService.GeneratePodcastAsync(
            new Interfaces.GeneratePodcastScriptRequest(
                UserId: request.UserId,
                Topic: request.Topic,
                Title: request.Title,
                ContextType: request.ContextType,
                ModelName: request.ModelName,
                Temperature: null, // use default
                MaxTokens: null // use default
            ), ct);

        if (!scriptResult.IsSuccess)
        {
            _logger.LogError("Failed to generate script: {Error}", scriptResult.ErrorMessage);
            return Result<PodcastExecutionResult>.Failure(scriptResult.ErrorMessage ?? "Script generation failed");
        }

        var script = scriptResult.Data!;
        _logger.LogInformation("Script generated: ID {ScriptId}", script.ScriptId);

        // 2. Convert to Audio using VieNeuTTS (via AudioService)
        var audioResult = await _audioService.GenerateAsync(
            new GenerateAudioFromScriptRequest(
                UserId: request.UserId,
                ScriptId: script.ScriptId,
                VoiceId: request.VoiceId,
                Speed: request.Speed,
                Pitch: request.Pitch
            ), ct);

        if (!audioResult.IsSuccess)
        {
            _logger.LogError("Failed to generate audio: {Error}", audioResult.ErrorMessage);
            return Result<PodcastExecutionResult>.Failure(audioResult.ErrorMessage ?? "Audio generation failed");
        }

        var audio = audioResult.Data!;
        _logger.LogInformation("Audio generated: path {Path}", audio.AudioPath);

        // 3. Sync to live-session-service
        var syncRequest = new PodcastSyncRequest(
            CreatedBy: request.UserId,
            Title: script.Title ?? request.Topic,
            Description: request.Topic, // Or take from LLM summary if available
            Author: null, // Can be user display name
            Type: "generated",
            Banner: null,
            AudioUrl: audio.AudioPath); // In prod, this should be transformed to a public URL or used via internal proxy

        var syncResponse = await _syncClient.SyncToLiveServiceAsync(syncRequest, ct);

        if (!syncResponse.Success)
        {
            _logger.LogWarning("Sync to live service failed: {Error}. Pipeline continues.", syncResponse.ErrorMessage);
        }

        return Result<PodcastExecutionResult>.Success(new PodcastExecutionResult(
            Script: script,
            Audio: audio,
            SyncedToLiveService: syncResponse.Success,
            LiveServicePodcastId: syncResponse.PodcastId
        ));
    }
}
