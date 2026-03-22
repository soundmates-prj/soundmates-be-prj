using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Domain.Interfaces;

public interface IMediaFileRepository
{
    Task<MediaFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MediaFile>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    Task<MediaFile?> GetByFilePathAsync(string filePath, CancellationToken cancellationToken = default);
    Task AddAsync(MediaFile mediaFile, CancellationToken cancellationToken = default);
    Task UpdateAsync(MediaFile mediaFile, CancellationToken cancellationToken = default);
    Task DeleteAsync(MediaFile mediaFile, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MediaFile>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MediaFile>> GetByFilePathsAsync(IReadOnlyCollection<string> filePaths, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MediaFile>> GetByStationIdAsync(Guid stationId, CancellationToken cancellationToken = default);
}
