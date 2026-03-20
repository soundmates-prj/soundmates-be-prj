using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Features.Users.Commands;
using AuthService.Application.Results;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Features.Users.Handlers;

/// <summary>
/// Handler xử lý nghiệp vụ xóa mục yêu thích của người dùng.
///
/// Luồng chính:
/// 1) Chuẩn hóa + validate input bằng Domain methods.
/// 2) Xóa bản ghi UserFavourite (PostgreSQL).
/// 3) Nếu Spotify: dọn cache spotify_items nếu không còn user nào tham chiếu.
/// 4) SaveChanges (PostgreSQL commit).
/// 5) Dual-write: xóa document trong MongoDB read-side (non-fatal).
/// </summary>
public sealed class DeleteUserFavouriteHandler : ICommandHandler<DeleteUserFavouriteCommand, bool>
{
    private readonly IUserFavouriteRepository _userFavouriteRepository;
    private readonly ISpotifyItemRepository _spotifyItemRepository;
    private readonly IFavouriteSyncRepository _favouriteSync;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteUserFavouriteHandler> _logger;

    public DeleteUserFavouriteHandler(
        IUserFavouriteRepository userFavouriteRepository,
        ISpotifyItemRepository spotifyItemRepository,
        IFavouriteSyncRepository favouriteSync,
        IUnitOfWork unitOfWork,
        ILogger<DeleteUserFavouriteHandler> logger)
    {
        _userFavouriteRepository = userFavouriteRepository;
        _spotifyItemRepository = spotifyItemRepository;
        _favouriteSync = favouriteSync;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteUserFavouriteCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Chuẩn hóa + validate input
            var itemType = UserFavourite.NormalizeItemType(command.ItemType);
            var itemId   = UserFavourite.NormalizeItemId(command.ItemId);
            var source   = UserFavourite.NormalizeSource(command.Source);

            // 2. Xóa favourite theo khóa nghiệp vụ
            var deleted = await _userFavouriteRepository.DeleteAsync(command.UserId, itemType, itemId, source);
            if (!deleted)
                return Result<bool>.Failure("Favourite item not found", 404);

            // 3. Nếu là Spotify: kiểm tra còn ai favorite item này không
            if (source == FavouriteSource.Spotify.ToString().ToLowerInvariant())
            {
                var stillReferenced = await _userFavouriteRepository.AnyByItemAsync(itemType, itemId, source);
                if (!stillReferenced)
                    await _spotifyItemRepository.DeleteAsync(itemId, itemType);
            }

            // 4. Commit toàn bộ thay đổi PostgreSQL
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 5. Dual-write: xóa document trong MongoDB read-side (non-fatal)
            _ = _favouriteSync.SyncDeleteAsync(
                command.UserId, itemType, itemId, source,
                CancellationToken.None          // Don't cancel sync on request abort
            ).ContinueWith(t => _logger.LogWarning(t.Exception,
                "Dual-write delete to MongoDB failed for user {UserId}, [{Type}/{Id}/{Source}].",
                command.UserId, itemType, itemId, source),
                TaskContinuationOptions.OnlyOnFaulted);

            return Result<bool>.Success(true, "Removed from favourites");
        }
        catch (DomainException ex)
        {
            return Result<bool>.Failure(ex.Message, ex.StatusCode);
        }
    }
}
