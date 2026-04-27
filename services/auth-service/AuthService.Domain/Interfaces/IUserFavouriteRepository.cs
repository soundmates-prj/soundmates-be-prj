using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;

public interface IUserFavouriteRepository
{
    Task<bool>            ExistsAsync(Guid userId, string itemType, string itemId, string source);
    Task<bool>            AnyByItemAsync(string itemType, string itemId, string source);

    /// <summary>Get a single favourite by its business key (userId + itemType + itemId + source).</summary>
    Task<UserFavourite?>  FindByKeyAsync(Guid userId, string itemType, string itemId, string source);

    Task AddAsync(UserFavourite userFavourite);
    Task<bool>            DeleteAsync(Guid userId, string itemType, string itemId, string source);
}
