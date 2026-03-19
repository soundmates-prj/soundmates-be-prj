using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Podcasts;

namespace LiveSessionService.Application.Features.Podcasts.Commands.UpdatePodcast;

public sealed record UpdatePodcastCommand(
    Guid PodcastId,
    string Title,
    string? Description,
    string? Author,
    string? Type,
    string? Banner,
    string? Status) : ICommand<PodcastResult>;
