using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Features.Users.Commands;

public sealed record CreateUserFavouriteCommand : ICommand<Guid>
{
    public required Guid UserId { get; init; }
    public required string ItemType { get; init; }
    public required string ItemId { get; init; }
    public string Source { get; init; } = "spotify";
    public string? Name { get; init; }
    public string? ArtistName { get; init; }
    public string? AlbumName { get; init; }
    public string? ImgUrl { get; init; }
    public string? PreviewUrl { get; init; }
    public string? RawJson { get; init; }
}
