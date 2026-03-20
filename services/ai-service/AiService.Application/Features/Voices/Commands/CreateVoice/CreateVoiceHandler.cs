using AiService.Application.Abstractions.Messaging;
using AiService.Application.Interfaces;
using AiService.Application.Results;
using AiService.Domain.Entities;

namespace AiService.Application.Features.Voices.Commands.CreateVoice;

public class CreateVoiceHandler : ICommandHandler<CreateVoiceCommand, TtsVoice>
{
    private readonly IVoiceService _voices;

    public CreateVoiceHandler(IVoiceService voices)
    {
        _voices = voices;
    }

    public Task<Result<TtsVoice>> Handle(CreateVoiceCommand command, CancellationToken cancellationToken)
        => _voices.CreateAsync(command.Voice, cancellationToken);
}

