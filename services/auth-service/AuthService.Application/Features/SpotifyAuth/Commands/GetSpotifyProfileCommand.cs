using AuthService.Application.Abstractions;
using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Features.SpotifyAuth.Commands;

public sealed record GetSpotifyProfileCommand : ICommand<SpotifyUserProfile>
{
    public required Guid UserId { get; init; }
}