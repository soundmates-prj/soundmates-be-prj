using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.DTOs;

namespace AuthQueryService.Application.Services.Favourites.Queries.GetMyFavourites
{
    /// <summary>
    /// Query: Get all favourites for the authenticated user.
    /// Supports optional filters for itemType and source.
    /// </summary>
    public sealed record GetMyFavouritesQuery(
        Guid UserId,
        string? ItemType = null,
        string? Source = null
    ) : IQuery<List<UserFavouriteDto>>;
}
