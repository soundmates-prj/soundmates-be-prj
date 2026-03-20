using AiService.Application.Abstractions.Messaging;

namespace AiService.Application.Features.Voices.Commands.CreateVoice;

public record CreateVoiceCommand(
    Guid UserId,
    bool IsUserVoice,
    string Provider,
    string? VoiceCode,
    string DisplayName,
    string Region,
    string Gender,
    string Model,
    bool IsActive) : ICommand<AiService.Domain.Entities.TtsVoice>;

