using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using LiveSessionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiveSessionService.Infrastructure.Repositories;

public sealed class MediaFileRepository : IMediaFileRepository
{
    private readonly LiveSessionDbContext _db;

    public MediaFileRepository(LiveSessionDbContext db) => _db = db;

    public Task<MediaFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.MediaFiles.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task AddAsync(MediaFile mediaFile, CancellationToken cancellationToken = default)
    {
        await _db.MediaFiles.AddAsync(mediaFile, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
