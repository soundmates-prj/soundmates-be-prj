using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;

public interface ISpotifyTokenRepository
{
    Task<SpotifyToken?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(SpotifyToken token, CancellationToken ct = default);
    Task UpdateAsync(SpotifyToken token, CancellationToken ct = default);
}
