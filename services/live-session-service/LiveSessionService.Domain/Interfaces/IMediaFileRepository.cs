using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Domain.Interfaces;

public interface IMediaFileRepository
{
    Task<MediaFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(MediaFile mediaFile, CancellationToken cancellationToken = default);
}
