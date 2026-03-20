using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Podcasts;

namespace LiveSessionService.Application.Features.Podcasts.Commands.CreatePodcast;

public sealed record CreatePodcastCommand(
    Guid CreatedBy,
    string Title,
    string? Description,
    string? Author,
    string? Type,
    string? Banner) : ICommand<PodcastResult>;
