using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.DTOs;
using AuthQueryService.Application.DTOs.Response;
using AuthQueryService.Domain.Interfaces;

namespace AuthQueryService.Application.Services.Favourites.Queries.GetMyFavourites
{
    /// <summary>
    /// Handles <see cref="GetMyFavouritesQuery"/>.
    /// Reads denormalized favourite documents from MongoDB and maps them to DTOs.
    /// No secondary Spotify lookup needed — metadata is embedded in the read model.
    /// </summary>
    public sealed class GetMyFavouritesQueryHandler
        : IQueryHandler<GetMyFavouritesQuery, List<UserFavouriteDto>>
    {
        private readonly IFavouriteReadRepository _repository;

        public GetMyFavouritesQueryHandler(IFavouriteReadRepository repository)
            => _repository = repository;

        public async Task<ApiResponse<List<UserFavouriteDto>>> Handle(
            GetMyFavouritesQuery query, CancellationToken cancellationToken)
        {
            var models = await _repository.GetByUserIdAsync(
                query.UserId,
                query.ItemType,
                query.Source,
                cancellationToken);

            var dtos = models.Select(m => new UserFavouriteDto
            {
                Id          = m.Id,
                UserId      = m.UserId,
                ItemType    = m.ItemType,
                ItemId      = m.ItemId,
                Source      = m.Source,
                Name        = m.Name,
                ArtistName  = m.ArtistName,
                AlbumName   = m.AlbumName,
                ImgUrl      = m.ImgUrl,
                PreviewUrl  = m.PreviewUrl,
                DurationMs  = m.DurationMs,
                ExternalUrl = m.ExternalUrl,
                CreatedAt   = m.CreatedAt
            }).ToList();

            return ApiResponse<List<UserFavouriteDto>>.SuccessResponse(
                dtos,
                $"Retrieved {dtos.Count} favourite(s).");
        }
    }
}
