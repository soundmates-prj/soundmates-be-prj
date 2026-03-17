using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;

public interface ISpotifyItemRepository
{
    Task<Guid> UpsertAsync(SpotifyItem item);
    Task<bool> DeleteAsync(string spotifyId, string itemType);
}
