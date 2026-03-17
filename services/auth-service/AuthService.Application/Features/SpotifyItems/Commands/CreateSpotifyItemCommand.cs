using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Features.SpotifyItems.Commands;

public sealed record CreateSpotifyItemCommand : ICommand<Guid>
{
    public required string SpotifyId { get; init; }
    public required string ItemType { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ArtistName { get; init; } = string.Empty;
    public string AlbumName { get; init; } = string.Empty;
    public string ImgUrl { get; init; } = string.Empty;
    public string PreviewUrl { get; init; } = string.Empty;
    public string RawJson { get; init; } = string.Empty;
}
