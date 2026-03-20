using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;

public class SpotifyItemRepository : ISpotifyItemRepository
{
    private readonly AuthDbContext _db;

    public SpotifyItemRepository(IUnitOfWork uow)
    {
        _db = (AuthDbContext)uow.Context;
    }

    public async Task<SpotifyItem?> GetByIdAsync(string spotifyId)
        => await _db.Set<SpotifyItem>().FirstOrDefaultAsync(x => x.SpotifyId == spotifyId);

    public async Task<Guid> UpsertAsync(SpotifyItem item)
    {
        var existing = await _db.Set<SpotifyItem>().FirstOrDefaultAsync(x =>
            x.SpotifyId == item.SpotifyId &&
            x.ItemType == item.ItemType);

        if (existing == null)
        {
            await _db.Set<SpotifyItem>().AddAsync(item);
            return item.Id;
        }

        existing.Name = item.Name;
        existing.ArtistName = item.ArtistName;
        existing.AlbumName = item.AlbumName;
        existing.ImgUrl = item.ImgUrl;
        existing.PreviewUrl = item.PreviewUrl;
        existing.RawJson = item.RawJson;
        existing.UpdatedAt = item.UpdatedAt;

        _db.Set<SpotifyItem>().Update(existing);
        return existing.Id;
    }

    public async Task<bool> DeleteAsync(string spotifyId, string itemType)
    {
        var existing = await _db.Set<SpotifyItem>().FirstOrDefaultAsync(x =>
            x.SpotifyId == spotifyId &&
            x.ItemType == itemType);

        if (existing == null)
            return false;

        _db.Set<SpotifyItem>().Remove(existing);
        return true;
    }
}
