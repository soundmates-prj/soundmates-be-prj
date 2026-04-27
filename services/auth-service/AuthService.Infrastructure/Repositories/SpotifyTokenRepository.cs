using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;

public class SpotifyTokenRepository : ISpotifyTokenRepository
{
    private readonly AuthDbContext _db;

    public SpotifyTokenRepository(IUnitOfWork uow)
    {
        _db = (AuthDbContext)uow.Context;
    }

    public async Task<SpotifyToken?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.SpotifyTokens.FirstOrDefaultAsync(t => t.UserId == userId, ct);
    }

    public async Task AddAsync(SpotifyToken token, CancellationToken ct = default)
    {
        await _db.SpotifyTokens.AddAsync(token, ct);
    }

    public Task UpdateAsync(SpotifyToken token, CancellationToken ct = default)
    {
        _db.SpotifyTokens.Update(token);
        return Task.CompletedTask;
    }
}
