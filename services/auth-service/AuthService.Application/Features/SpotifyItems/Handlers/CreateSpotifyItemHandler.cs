using AuthService.Application.Abstractions.Messaging;
using AuthService.Application.Features.Common;
using AuthService.Application.Features.SpotifyItems.Commands;
using AuthService.Application.Results;
using AuthService.Domain.Entities;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Features.SpotifyItems.Handlers;

public sealed class CreateSpotifyItemHandler : ICommandHandler<CreateSpotifyItemCommand, Guid>
{
    private readonly ISpotifyItemRepository _spotifyItemRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSpotifyItemHandler(
        ISpotifyItemRepository spotifyItemRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _spotifyItemRepository = spotifyItemRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateSpotifyItemCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var spotifyItem = SpotifyItem.Create(
                command.SpotifyId,
                command.ItemType,
                command.Name,
                command.ArtistName,
                command.AlbumName,
                command.ImgUrl,
                command.PreviewUrl,
                command.RawJson,
                _dateTimeProvider);

            var spotifyItemId = await _spotifyItemRepository.UpsertAsync(spotifyItem);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(spotifyItemId, "Create spotify item successful");
        }
        catch (DomainException ex)
        {
            return Result<Guid>.Failure(ex.Message, ex.StatusCode);
        }
    }
}
