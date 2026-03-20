using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Features.Users.Commands;
using AuthService.Application.Results;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Features.Users.Handlers;

/// <summary>
/// Handler xử lý nghiệp vụ xóa mục yêu thích của người dùng.
/// 
/// Lưu ý nghiệp vụ:
/// - Nếu favourite thuộc nguồn Spotify và không còn user nào tham chiếu,
///   hệ thống sẽ xóa luôn bản ghi cache trong <c>spotify_items</c>.
/// - SaveChanges được thực hiện 1 lần ở cuối handler theo UnitOfWork pattern.
/// </summary>
public sealed class DeleteUserFavouriteHandler : ICommandHandler<DeleteUserFavouriteCommand, bool>
{
    private readonly IUserFavouriteRepository _userFavouriteRepository;
    private readonly ISpotifyItemRepository _spotifyItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteUserFavouriteHandler(
        IUserFavouriteRepository userFavouriteRepository,
        ISpotifyItemRepository spotifyItemRepository,
        IUnitOfWork unitOfWork)
    {
        _userFavouriteRepository = userFavouriteRepository;
        _spotifyItemRepository = spotifyItemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteUserFavouriteCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Chuẩn hóa + validate input bằng Domain methods
            var itemType = UserFavourite.NormalizeItemType(command.ItemType);
            var itemId = UserFavourite.NormalizeItemId(command.ItemId);
            var source = UserFavourite.NormalizeSource(command.Source);

            // 2. Xóa favourite theo khóa nghiệp vụ
            var deleted = await _userFavouriteRepository.DeleteAsync(command.UserId, itemType, itemId, source);
            if (!deleted)
                return Result<bool>.Failure("Favourite item not found", 404);

            // 3. Nếu là Spotify thì kiểm tra còn ai favorite item này không
            if (source == FavouriteSource.Spotify.ToString().ToLowerInvariant())
            {
                var stillReferenced = await _userFavouriteRepository.AnyByItemAsync(itemType, itemId, source);
                if (!stillReferenced)
                {
                    // Không còn tham chiếu => dọn dữ liệu cache spotify_items
                    await _spotifyItemRepository.DeleteAsync(itemId, itemType);
                }
            }

            // 4. Commit toàn bộ thay đổi tại một transaction boundary
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(true, "Removed from favourites");
        }
        catch (DomainException ex)
        {
            // Forward lỗi nghiệp vụ ra tầng API theo chuẩn Result
            return Result<bool>.Failure(ex.Message, ex.StatusCode);
        }
    }
}
