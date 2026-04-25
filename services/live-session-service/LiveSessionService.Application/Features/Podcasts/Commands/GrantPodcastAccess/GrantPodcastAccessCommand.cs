using LiveSessionService.Application.Abstractions.Messaging;
using System;

namespace LiveSessionService.Application.Features.Podcasts.Commands.GrantPodcastAccess;

public sealed record GrantPodcastAccessCommand(Guid PodcastId, Guid UserId, decimal Price) : ICommand;
