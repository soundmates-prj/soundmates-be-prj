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
/// Handler xử lý nghiệp vụ thêm một mục yêu thích cho người dùng.
///
/// Luồng chính:
/// 1) Kiểm tra user tồn tại.
/// 2) Chuẩn hóa + validate dữ liệu bằng Domain methods (trả về enum-backed string).
/// 3) Kiểm tra trùng favourite.
/// 4) Tạo bản ghi UserFavourite (PostgreSQL — source of truth).
/// 5) Nếu nguồn Spotify:
///    5a) Tự động fetch metadata thật từ Spotify API qua <see cref="ISpotifyApiClient"/>.
///        Nếu Spotify gọi lỗi → fallback sang giá trị client cung cấp (non-fatal).
///    5b) Upsert cache SpotifyItem với metadata đã enrich.
/// 6) Commit vào PostgreSQL qua UnitOfWork (single transaction boundary).
/// 7) Dual-write vào MongoDB read-side (fire-and-forget, non-fatal).
/// </summary>
public sealed class CreateUserFavouriteHandler : ICommandHandler<CreateUserFavouriteCommand, Guid>
{
    private readonly IUserRepository             _userRepo;
    private readonly IUserFavouriteRepository    _favRepo;
    private readonly ISpotifyItemRepository      _spotifyItemRepo;
    private readonly ISpotifyApiClient           _spotifyClient;
    private readonly IFavouriteSyncRepository    _favouriteSync;
    private readonly IDateTimeProvider           _clock;
    private readonly IUnitOfWork                 _uow;
    private readonly ILogger<CreateUserFavouriteHandler> _logger;

    public CreateUserFavouriteHandler(
        IUserRepository             userRepo,
        IUserFavouriteRepository    favRepo,
        ISpotifyItemRepository      spotifyItemRepo,
        ISpotifyApiClient           spotifyClient,
        IFavouriteSyncRepository    favouriteSync,
        IDateTimeProvider           clock,
        IUnitOfWork                 uow,
        ILogger<CreateUserFavouriteHandler> logger)
    {
        _userRepo        = userRepo;
        _favRepo         = favRepo;
        _spotifyItemRepo = spotifyItemRepo;
        _spotifyClient   = spotifyClient;
        _favouriteSync   = favouriteSync;
        _clock           = clock;
        _uow             = uow;
        _logger          = logger;
    }

    public async Task<Result<Guid>> Handle(
        CreateUserFavouriteCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // 1. User phải tồn tại
            var user = await _userRepo.GetByIdAsync(command.UserId);
            if (user is null)
                return Result<Guid>.Failure("User not found", 404);

            // 2. Chuẩn hóa — Domain throws DomainException on invalid input
            var itemType = UserFavourite.NormalizeItemType(command.ItemType); // e.g. "track"
            var itemId   = UserFavourite.NormalizeItemId(command.ItemId);
            var source   = UserFavourite.NormalizeSource(command.Source);     // e.g. "spotify"

            // 3. Trùng lặp
            if (await _favRepo.ExistsAsync(command.UserId, itemType, itemId, source))
                return Result<Guid>.Failure("Item already exists in favourites", 409);

            // 4. Tạo entity
            var favourite = UserFavourite.Create(command.UserId, itemType, itemId, source, _clock);
            await _favRepo.AddAsync(favourite);

            // 5. Metadata — dùng giá trị client làm fallback, Spotify làm nguồn sự thật
            var meta = new FavouriteMeta(
                command.Name, command.ArtistName, command.AlbumName,
                command.ImgUrl, command.PreviewUrl);

            if (source == FavouriteSource.Spotify.ToString().ToLowerInvariant())
            {
                // 5a. Parse itemType thành FavouriteItemType enum (không magic string)
                //     NormalizeItemType đã validate nên TryParse always succeeds here
                Enum.TryParse<FavouriteItemType>(itemType, ignoreCase: true, out var itemTypeEnum);
                meta = await EnrichFromSpotifyAsync(itemTypeEnum, itemId, meta, cancellationToken);

                // 5b. Upsert Spotify metadata cache
                var spotifyItem = SpotifyItem.Create(
                    itemId, itemType,
                    meta.Name, meta.ArtistName, meta.AlbumName,
                    meta.ImgUrl, meta.PreviewUrl,
                    command.RawJson,
                    _clock);

                await _spotifyItemRepo.UpsertAsync(spotifyItem);
            }

            // 6. Commit PostgreSQL
            await _uow.SaveChangesAsync(cancellationToken);

            // 7. Dual-write MongoDB read-side (fire-and-forget, non-fatal)
            _ = _favouriteSync.SyncUpsertAsync(
                id:          favourite.Id,
                userId:      command.UserId,
                itemType:    itemType,
                itemId:      itemId,
                source:      source,
                name:        meta.Name,
                artistName:  meta.ArtistName,
                albumName:   meta.AlbumName,
                imgUrl:      meta.ImgUrl,
                previewUrl:  meta.PreviewUrl,
                durationMs:  meta.DurationMs,
                externalUrl: meta.ExternalUrl,
                createdAt:   favourite.CreatedAt,
                ct:          CancellationToken.None
            ).ContinueWith(t =>
                _logger.LogWarning(t.Exception,
                    "Dual-write to MongoDB failed for favourite {Id}.", favourite.Id),
                TaskContinuationOptions.OnlyOnFaulted);

            return Result<Guid>.Success(favourite.Id, "Create user favourite successful");
        }
        catch (DomainException ex)
        {
            return Result<Guid>.Failure(ex.Message, ex.StatusCode);
        }
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Fetches real display metadata from Spotify for supported item types.
    /// Returns the original <paramref name="fallback"/> if Spotify is unreachable or returns error.
    /// </summary>
    private async Task<FavouriteMeta> EnrichFromSpotifyAsync(
        FavouriteItemType itemType, string itemId,
        FavouriteMeta fallback, CancellationToken ct)
    {
        try
        {
            return itemType switch
            {
                FavouriteItemType.Track => await EnrichTrackAsync(itemId, fallback, ct),
                FavouriteItemType.Artist => await EnrichArtistAsync(itemId, fallback, ct),
                FavouriteItemType.Album => await EnrichAlbumAsync(itemId, fallback, ct),
                // Playlist, Podcast, Episode, Show, etc. — pass through client data for now
                _ => fallback
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to fetch Spotify metadata for {Type}/{Id}. Using client-provided data.",
                itemType, itemId);
            return fallback;
        }
    }

    private async Task<FavouriteMeta> EnrichTrackAsync(
        string trackId, FavouriteMeta fallback, CancellationToken ct)
    {
        var track = await _spotifyClient.GetTrackAsync(trackId, ct);
        return track is null ? fallback : new FavouriteMeta(
            track.Name, track.ArtistName, track.AlbumName,
            track.ImgUrl, track.PreviewUrl,
            track.DurationMs, track.ExternalUrl);
    }

    private async Task<FavouriteMeta> EnrichArtistAsync(
        string artistId, FavouriteMeta fallback, CancellationToken ct)
    {
        var artist = await _spotifyClient.GetArtistAsync(artistId, ct);
        return artist is null ? fallback : new FavouriteMeta(
            artist.Name, artist.Name, AlbumName: null,
            artist.ImgUrl, PreviewUrl: null,
            DurationMs: null, artist.ExternalUrl);
    }

    private async Task<FavouriteMeta> EnrichAlbumAsync(
        string albumId, FavouriteMeta fallback, CancellationToken ct)
    {
        var album = await _spotifyClient.GetAlbumAsync(albumId, ct);
        return album is null ? fallback : new FavouriteMeta(
            album.Name, album.ArtistName, AlbumName: album.Name,
            album.ImgUrl, PreviewUrl: null,
            DurationMs: null, album.ExternalUrl);
    }

    // ── Value object for enriched metadata ────────────────────────────────────
    private record FavouriteMeta(
        string? Name,
        string? ArtistName,
        string? AlbumName,
        string? ImgUrl,
        string? PreviewUrl,
        int?    DurationMs  = null,
        string? ExternalUrl = null);
}
