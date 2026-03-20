using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Features.SpotifyAuth.Commands;

public sealed record GetSpotifyLoginUrlCommand : ICommand<string>;
