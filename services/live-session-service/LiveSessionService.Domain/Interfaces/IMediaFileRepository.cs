using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Domain.Interfaces;

public interface IMediaFileRepository
{
    Task<MediaFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(MediaFile mediaFile, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MediaFile>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MediaFile>> GetByStationIdAsync(Guid stationId, CancellationToken cancellationToken = default);
}
