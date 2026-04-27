using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;

public class UserFavouriteRepository : IUserFavouriteRepository
{
    private readonly IUnitOfWork _uow;
    private readonly AuthDbContext _db;

    public UserFavouriteRepository(IUnitOfWork uow)
    {
        _uow = uow;
        _db = (AuthDbContext)_uow.Context;
    }

    public async Task<bool> ExistsAsync(Guid userId, string itemType, string itemId, string source)
    {
        return await _db.UserFavourites.AnyAsync(x =>
            x.UserId == userId &&
            x.ItemType == itemType &&
            x.ItemId == itemId &&
            x.Source == source);
    }

    public async Task<bool> AnyByItemAsync(string itemType, string itemId, string source)
    {
        return await _db.UserFavourites.AnyAsync(x =>
            x.ItemType == itemType &&
            x.ItemId == itemId &&
            x.Source == source);
    }

    public async Task AddAsync(UserFavourite userFavourite)
    {
        await _db.UserFavourites.AddAsync(userFavourite);
    }

    public async Task<UserFavourite?> FindByKeyAsync(
        Guid userId, string itemType, string itemId, string source)
    {
        return await _db.UserFavourites.FirstOrDefaultAsync(x =>
            x.UserId   == userId   &&
            x.ItemType == itemType &&
            x.ItemId   == itemId   &&
            x.Source   == source);
    }

    public async Task<bool> DeleteAsync(Guid userId, string itemType, string itemId, string source)
    {
        var entity = await _db.UserFavourites.FirstOrDefaultAsync(x =>
            x.UserId == userId &&
            x.ItemType == itemType &&
            x.ItemId == itemId &&
            x.Source == source);

        if (entity == null)
            return false;

        _db.UserFavourites.Remove(entity);
        return true;
    }
}
