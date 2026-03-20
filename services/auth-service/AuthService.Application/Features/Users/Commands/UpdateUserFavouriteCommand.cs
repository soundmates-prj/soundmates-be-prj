using AuthService.Application.Abstractions.Messaging;

namespace AuthService.Application.Features.Users.Commands;

/// <summary>
/// Cập nhật metadata của một mục yêu thích hiện có.
///
/// Nếu source = Spotify và refreshFromSpotify = true,
/// hệ thống sẽ tự động fetch lại metadata thật từ Spotify API
/// và ghi đè tất cả các trường metadata.
///
/// Nếu refreshFromSpotify = false (mặc định), metadata được cập nhật
/// từ các trường mà client gửi lên (partial update - chỉ các trường khác null).
/// </summary>
public sealed record UpdateUserFavouriteCommand : ICommand<bool>
{
    public required Guid   UserId   { get; init; }

    // Business key — identifies WHICH favourite to update
    public required string ItemType { get; init; }
    public required string ItemId   { get; init; }
    public required string Source   { get; init; }

    // Metadata to overwrite (null = keep existing)
    public string? Name        { get; init; }
    public string? ArtistName  { get; init; }
    public string? AlbumName   { get; init; }
    public string? ImgUrl      { get; init; }
    public string? PreviewUrl  { get; init; }

    /// <summary>
    /// When true, ignores client-provided metadata and re-fetches from Spotify.
    /// Useful for "refresh" actions in the UI.
    /// </summary>
    public bool RefreshFromSpotify { get; init; } = false;
}
