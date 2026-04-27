using AuthService.Application.Abstractions;
using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Features.Common;
using AuthService.Application.Features.Users.Commands;
using AuthService.Application.Results;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Users.Handlers;

/// <summary>
/// Handler cập nhật metadata của một mục yêu thích.
///
/// Luồng chính:
/// 1) Chuẩn hóa + validate business key.
/// 2) Tìm favourite hiện có (404 nếu không có).
/// 3a) Nếu RefreshFromSpotify = true → gọi Spotify API để lấy metadata thật.
/// 3b) Nếu RefreshFromSpotify = false → dùng giá trị từ request (partial patch).
/// 4) Cập nhật SpotifyItem cache (PostgreSQL).
/// 5) Cập nhật UpdatedAt của UserFavourite (để track lần cuối sync).
/// 6) SaveChanges (PostgreSQL).
/// 7) Dual-write: sync metadata mới sang MongoDB read-side (non-fatal).
/// </summary>
public sealed class UpdateUserFavouriteHandler : ICommandHandler<UpdateUserFavouriteCommand, bool>
{
    private readonly IUserFavouriteRepository _favRepo;
    private readonly ISpotifyItemRepository   _spotifyItemRepo;
    private readonly ISpotifyApiClient        _spotifyClient;
    private readonly IFavouriteSyncRepository _favouriteSync;
    private readonly IDateTimeProvider        _clock;
    private readonly IUnitOfWork              _uow;
    private readonly ILogger<UpdateUserFavouriteHandler> _logger;

    public UpdateUserFavouriteHandler(
        IUserFavouriteRepository favRepo,
        ISpotifyItemRepository   spotifyItemRepo,
        ISpotifyApiClient        spotifyClient,
        IFavouriteSyncRepository favouriteSync,
        IDateTimeProvider        clock,
        IUnitOfWork              uow,
        ILogger<UpdateUserFavouriteHandler> logger)
    {
        _favRepo         = favRepo;
        _spotifyItemRepo = spotifyItemRepo;
        _spotifyClient   = spotifyClient;
        _favouriteSync   = favouriteSync;
        _clock           = clock;
        _uow             = uow;
        _logger          = logger;
    }

    public async Task<Result<bool>> Handle(
        UpdateUserFavouriteCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Chuẩn hoá business key
            var itemType = UserFavourite.NormalizeItemType(command.ItemType);
            var itemId   = UserFavourite.NormalizeItemId(command.ItemId);
            var source   = UserFavourite.NormalizeSource(command.Source);

            // 2. Kiểm tra favourite tồn tại
            var favourite = await _favRepo.FindByKeyAsync(
                command.UserId, itemType, itemId, source);
            if (favourite is null)
                return Result<bool>.Failure("Favourite not found", 404);

            // 3. Resolve metadata
            string? name        = command.Name;
            string? artistName  = command.ArtistName;
            string? albumName   = command.AlbumName;
            string? imgUrl      = command.ImgUrl;
            string? previewUrl  = command.PreviewUrl;
            int?    durationMs  = null;
            string? externalUrl = null;

            var isSpotify = source == FavouriteSource.Spotify.ToString().ToLowerInvariant();

            if (isSpotify && command.RefreshFromSpotify)
            {
                // 3a. Fetch real metadata from Spotify
                Enum.TryParse<FavouriteItemType>(itemType, ignoreCase: true, out var itemTypeEnum);
                (name, artistName, albumName, imgUrl, previewUrl, durationMs, externalUrl) =
                    await FetchSpotifyMetadataAsync(itemTypeEnum, itemId, cancellationToken);
            }

            // 4. Update SpotifyItem cache if there's any new metadata to persist
            if (isSpotify && HasAnyValue(name, artistName, albumName, imgUrl, previewUrl))
            {
                var existing = await _spotifyItemRepo.GetByIdAsync(itemId);
                if (existing is not null)
                {
                    existing.Name       = name       ?? existing.Name;
                    existing.ArtistName = artistName ?? existing.ArtistName;
                    existing.AlbumName  = albumName  ?? existing.AlbumName;
                    existing.ImgUrl     = imgUrl     ?? existing.ImgUrl;
                    existing.PreviewUrl = previewUrl ?? existing.PreviewUrl;
                    existing.UpdatedAt  = _clock.UtcNow;
                }
            }

            // 5. Touch UpdatedAt on UserFavourite (audit trail)
            favourite.UpdatedAt = _clock.UtcNow;

            // 6. Commit PostgreSQL
            await _uow.SaveChangesAsync(cancellationToken);

            // 7. Dual-write: patch MongoDB document (fire-and-forget, non-fatal)
            _ = _favouriteSync.SyncUpdateAsync(
                favouriteId: favourite.Id,
                name:        name,
                artistName:  artistName,
                albumName:   albumName,
                imgUrl:      imgUrl,
                previewUrl:  previewUrl,
                durationMs:  durationMs,
                externalUrl: externalUrl,
                updatedAt:   favourite.UpdatedAt!.Value,
                ct:          CancellationToken.None
            ).ContinueWith(t =>
                _logger.LogWarning(t.Exception,
                    "Dual-write update to MongoDB failed for favourite {Id}.", favourite.Id),
                TaskContinuationOptions.OnlyOnFaulted);

            return Result<bool>.Success(true, "Favourite updated successfully");
        }
        catch (DomainException ex)
        {
            return Result<bool>.Failure(ex.Message, ex.StatusCode);
        }
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private async Task<(string? Name, string? Artist, string? Album,
                         string? Img, string? Preview, int? Duration, string? ExtUrl)>
        FetchSpotifyMetadataAsync(FavouriteItemType itemType, string itemId, CancellationToken ct)
    {
        try
        {
            return itemType switch
            {
                FavouriteItemType.Track => await FetchTrackAsync(itemId, ct),
                FavouriteItemType.Artist => await FetchArtistAsync(itemId, ct),
                FavouriteItemType.Album => await FetchAlbumAsync(itemId, ct),
                _ => (null, null, null, null, null, null, null)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to fetch Spotify metadata for {Type}/{Id} during update.",
                itemType, itemId);
            return (null, null, null, null, null, null, null);
        }
    }

    private async Task<(string?, string?, string?, string?, string?, int?, string?)>
        FetchTrackAsync(string trackId, CancellationToken ct)
    {
        var t = await _spotifyClient.GetTrackAsync(trackId, ct);
        return t is null
            ? (null, null, null, null, null, null, null)
            : (t.Name, t.ArtistName, t.AlbumName, t.ImgUrl, t.PreviewUrl, t.DurationMs, t.ExternalUrl);
    }

    private async Task<(string?, string?, string?, string?, string?, int?, string?)>
        FetchArtistAsync(string artistId, CancellationToken ct)
    {
        var a = await _spotifyClient.GetArtistAsync(artistId, ct);
        return a is null
            ? (null, null, null, null, null, null, null)
            : (a.Name, a.Name, null, a.ImgUrl, null, null, a.ExternalUrl);
    }

    private async Task<(string?, string?, string?, string?, string?, int?, string?)>
        FetchAlbumAsync(string albumId, CancellationToken ct)
    {
        var a = await _spotifyClient.GetAlbumAsync(albumId, ct);
        return a is null
            ? (null, null, null, null, null, null, null)
            : (a.Name, a.ArtistName, a.Name, a.ImgUrl, null, null, a.ExternalUrl);
    }

    private static bool HasAnyValue(params string?[] values)
        => values.Any(v => v is not null);
}
