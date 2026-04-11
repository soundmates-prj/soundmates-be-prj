using AiService.Application.Abstractions.Messaging;
using AiService.Domain.Entities;

namespace AiService.Application.Features.Audios.Commands.GenerateAudio;

public record GenerateAudioFromScriptCommand(
    Guid UserId,
    Guid ScriptId,
    string VoiceCode,
    decimal? Speed,
    decimal? Pitch) : ICommand<ScriptAudio>;

