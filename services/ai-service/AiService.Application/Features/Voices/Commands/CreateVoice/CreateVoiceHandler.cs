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

    public async Task<Result<TtsVoice>> Handle(CreateVoiceCommand command, CancellationToken cancellationToken)
    {
        // Voice code format differs for built-in vs user-generated voices.
        // Keep this logic inside Application so API controllers stay thin.
        var normalizedProvider = command.IsUserVoice
            ? "user"
            : (command.Provider?.Trim().ToLowerInvariant() ?? "vienetts");

        string voiceCode;
        if (command.IsUserVoice)
        {
            voiceCode = $"user-{command.UserId:N}-{Guid.NewGuid():N}";
        }
        else
        {
            voiceCode = string.IsNullOrWhiteSpace(command.VoiceCode) ? string.Empty : command.VoiceCode.Trim();
            if (string.IsNullOrWhiteSpace(voiceCode))
                return Result<TtsVoice>.Failure("voiceCode is required for built-in voice");
        }

        var voice = new TtsVoice
        {
            Provider = normalizedProvider,
            VoiceCode = voiceCode,
            DisplayName = command.DisplayName.Trim(),
            Region = command.Region.Trim(),
            Gender = command.Gender.Trim(),
            Model = command.Model.Trim(),
            IsActive = command.IsActive
        };

        return await _voices.CreateAsync(voice, cancellationToken);
    }
}

