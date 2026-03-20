using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Features.SpotifyItems.Commands;
using AuthService.Application.Results;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Features.SpotifyItems.Handlers;

public sealed class DeleteSpotifyItemHandler : ICommandHandler<DeleteSpotifyItemCommand, bool>
{
    private readonly ISpotifyItemRepository _spotifyItemRepository;
    private readonly IUserFavouriteRepository _userFavouriteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSpotifyItemHandler(
        ISpotifyItemRepository spotifyItemRepository,
        IUserFavouriteRepository userFavouriteRepository,
        IUnitOfWork unitOfWork)
    {
        _spotifyItemRepository = spotifyItemRepository;
        _userFavouriteRepository = userFavouriteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteSpotifyItemCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var spotifyId = SpotifyItem.NormalizeSpotifyId(command.SpotifyId);
            var itemType = SpotifyItem.NormalizeItemType(command.ItemType);

            var spotifySource = FavouriteSource.Spotify.ToString().ToLowerInvariant();
            var isReferenced = await _userFavouriteRepository.AnyByItemAsync(itemType, spotifyId, spotifySource);
            if (isReferenced)
                return Result<bool>.Failure("Cannot delete Spotify item because it is still in user favourites", 409);

            var deleted = await _spotifyItemRepository.DeleteAsync(spotifyId, itemType);
            if (!deleted)
                return Result<bool>.Failure("Spotify item not found", 404);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(true, "Spotify item deleted");
        }
        catch (DomainException ex)
        {
            return Result<bool>.Failure(ex.Message, ex.StatusCode);
        }
    }
}
