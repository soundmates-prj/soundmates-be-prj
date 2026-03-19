using AiService.Application.Abstractions.Messaging;
using AiService.Application.Interfaces;
using AiService.Application.Results;
using AiService.Domain.Entities;

namespace AiService.Application.Features.Audios.Commands.GenerateAudio;

public class GenerateAudioFromScriptHandler : ICommandHandler<GenerateAudioFromScriptCommand, ScriptAudio>
{
    private readonly IAudioService _audios;

    public GenerateAudioFromScriptHandler(IAudioService audios)
    {
        _audios = audios;
    }

    public Task<Result<ScriptAudio>> Handle(GenerateAudioFromScriptCommand command, CancellationToken cancellationToken)
        => _audios.GenerateAsync(
            new GenerateAudioFromScriptRequest(
                UserId: command.UserId,
                ScriptId: command.ScriptId,
                VoiceId: command.VoiceId,
                Speed: command.Speed,
                Pitch: command.Pitch),
            cancellationToken);
}

