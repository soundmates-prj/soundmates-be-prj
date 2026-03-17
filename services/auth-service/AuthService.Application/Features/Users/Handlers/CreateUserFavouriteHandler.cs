using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Features.Common;
using AuthService.Application.Features.Users.Commands;
using AuthService.Application.Results;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Features.Users.Handlers;

/// <summary>
/// Handler xử lý nghiệp vụ thêm một mục yêu thích cho người dùng hiện tại.
/// 
/// Luồng chính:
/// 1) Kiểm tra người dùng tồn tại.
/// 2) Chuẩn hóa + validate dữ liệu bằng Domain methods.
/// 3) Kiểm tra trùng favourite.
/// 4) Tạo bản ghi <see cref="UserFavourite"/>.
/// 5) Nếu nguồn là Spotify thì upsert thêm cache <see cref="SpotifyItem"/>.
/// 6) Commit 1 lần qua UnitOfWork để đảm bảo transaction boundary rõ ràng.
/// </summary>
public sealed class CreateUserFavouriteHandler : ICommandHandler<CreateUserFavouriteCommand, Guid>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserFavouriteRepository _userFavouriteRepository;
    private readonly ISpotifyItemRepository _spotifyItemRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserFavouriteHandler(
        IUserRepository userRepository,
        IUserFavouriteRepository userFavouriteRepository,
        ISpotifyItemRepository spotifyItemRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _userFavouriteRepository = userFavouriteRepository;
        _spotifyItemRepository = spotifyItemRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateUserFavouriteCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Bảo vệ nghiệp vụ: chỉ thao tác khi user tồn tại
            var user = await _userRepository.GetByIdAsync(command.UserId);
            if (user == null)
                return Result<Guid>.Failure("User not found", 404);

            // 2. Chuẩn hóa dữ liệu đầu vào theo Domain rules (enum + độ dài + required)
            var itemType = UserFavourite.NormalizeItemType(command.ItemType);
            var itemId = UserFavourite.NormalizeItemId(command.ItemId);
            var source = UserFavourite.NormalizeSource(command.Source);

            // 3. Không cho thêm trùng cùng (userId, itemType, itemId, source)
            var exists = await _userFavouriteRepository.ExistsAsync(command.UserId, itemType, itemId, source);
            if (exists)
                return Result<Guid>.Failure("Item already exists in favourites", 409);

            // 4. Tạo aggregate/entity từ Domain factory để đảm bảo invariant
            var favourite = UserFavourite.Create(
                command.UserId,
                itemType,
                itemId,
                source,
                _dateTimeProvider);

            await _userFavouriteRepository.AddAsync(favourite);

            // 5. Với nguồn Spotify: đồng bộ thêm metadata vào bảng cache spotify_items
            if (source == FavouriteSource.Spotify.ToString().ToLowerInvariant())
            {
                var spotifyItem = SpotifyItem.Create(
                    itemId,
                    itemType,
                    command.Name,
                    command.ArtistName,
                    command.AlbumName,
                    command.ImgUrl,
                    command.PreviewUrl,
                    command.RawJson,
                    _dateTimeProvider);

                await _spotifyItemRepository.UpsertAsync(spotifyItem);
            }

            // 6. Commit duy nhất tại handler (đúng UnitOfWork pattern)
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(favourite.Id, "Create user favourite successful");
        }
        catch (DomainException ex)
        {
            // DomainException trả về thông điệp nghiệp vụ + status code đã chuẩn hóa
            return Result<Guid>.Failure(ex.Message, ex.StatusCode);
        }
    }
}
