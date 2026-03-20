using AiService.Application.Abstractions.Messaging;
using AiService.Domain.Entities;

namespace AiService.Application.Features.Voices.Commands.CreateVoice;

public record CreateVoiceCommand(TtsVoice Voice) : ICommand<TtsVoice>;

