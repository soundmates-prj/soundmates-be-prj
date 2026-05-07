using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using AccountContentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccountContentService.Infrastructure.Repositories;

public class UserProfileReadModelRepository : IUserProfileReadModelRepository
{
    private readonly AccountContentDbContext _context;

    public UserProfileReadModelRepository(AccountContentDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfileReadModel?> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.UserProfileReadModels
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, ct);
    }

    public async Task UpsertAsync(UserProfileReadModel profile, CancellationToken ct = default)
    {
        var existing = await _context.UserProfileReadModels
            .FirstOrDefaultAsync(x => x.Id == profile.Id, ct);

        if (existing == null)
        {
            _context.UserProfileReadModels.Add(profile);
        }
        else
        {
            existing.FullName = profile.FullName;
            existing.FirstName = profile.FirstName;
            existing.LastName = profile.LastName;
            existing.AvatarUrl = profile.AvatarUrl;
            existing.Email = profile.Email;
            existing.IsPending = profile.IsPending;
            existing.UpdatedAt = profile.UpdatedAt;
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid userId, CancellationToken ct = default)
    {
        var existing = await _context.UserProfileReadModels
            .FirstOrDefaultAsync(x => x.Id == userId, ct);

        if (existing != null)
        {
            _context.UserProfileReadModels.Remove(existing);
            await _context.SaveChangesAsync(ct);
        }
    }
    public async Task<IEnumerable<UserProfileReadModel>> GetByIdsAsync(
    IEnumerable<Guid> userIds,
    CancellationToken cancellationToken)
    {
        return await _context.UserProfileReadModels
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken);
    }
}
