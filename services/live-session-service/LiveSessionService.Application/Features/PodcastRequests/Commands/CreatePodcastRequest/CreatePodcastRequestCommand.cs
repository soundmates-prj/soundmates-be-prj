using LiveSessionService.Application.Abstractions.Messaging;

namespace LiveSessionService.Application.Features.PodcastRequests.Commands.CreatePodcastRequest;

public sealed record CreatePodcastRequestCommand(
    Guid RequestedByUserId,
    Guid LiveSessionId,
    string Title,
    string? Description,
    string ScriptText,
    string AudioUrl,
    int DurationSeconds,
    string VoiceCode,
    string? VoiceDisplayName) : ICommand<Application.Features.Results.PodcastRequests.PodcastRequestResult>;