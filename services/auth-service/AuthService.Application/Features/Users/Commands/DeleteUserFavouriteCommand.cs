using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Features.Users.Commands;

public sealed record DeleteUserFavouriteCommand : ICommand<bool>
{
    public required Guid UserId { get; init; }
    public required string ItemType { get; init; }
    public required string ItemId { get; init; }
    public string Source { get; init; } = "spotify";
}
