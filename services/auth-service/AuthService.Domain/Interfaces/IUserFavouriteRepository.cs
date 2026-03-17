using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;

public interface IUserFavouriteRepository
{
    Task<bool> ExistsAsync(Guid userId, string itemType, string itemId, string source);
    Task<bool> AnyByItemAsync(string itemType, string itemId, string source);
    Task AddAsync(UserFavourite userFavourite);
    Task<bool> DeleteAsync(Guid userId, string itemType, string itemId, string source);
}
