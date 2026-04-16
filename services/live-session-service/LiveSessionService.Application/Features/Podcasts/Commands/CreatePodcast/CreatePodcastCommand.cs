using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.Podcasts;
using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Application.Features.Podcasts.Commands.CreatePodcast;

public sealed record CreatePodcastCommand(
    Guid CreatedBy,
    string Title,
    string? Description,
    string? Author,
    PodcastStatus Status,
    string? Type,
    string? Banner) : ICommand<PodcastResult>;
