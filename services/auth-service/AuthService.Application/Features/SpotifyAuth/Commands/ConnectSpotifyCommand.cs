using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Features.SpotifyAuth.Commands;

public sealed record ConnectSpotifyCommand : ICommand<bool>
{
    public required Guid UserId { get; init; }
    public required string Code { get; init; }
}