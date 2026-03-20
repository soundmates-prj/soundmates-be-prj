using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Features.SpotifyItems.Commands;

public sealed record DeleteSpotifyItemCommand : ICommand<bool>
{
    public required string SpotifyId { get; init; }
    public required string ItemType { get; init; }
}
