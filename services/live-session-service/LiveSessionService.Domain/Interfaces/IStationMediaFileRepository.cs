using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Domain.Interfaces;

public interface IStationMediaFileRepository
{
    Task<StationMediaFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<StationMediaFile>> GetByStationIdAsync(Guid stationId, CancellationToken cancellationToken = default);
    Task<List<StationMediaFile>> GetByMediaFileIdAsync(Guid mediaFileId, CancellationToken cancellationToken = default);
    Task<StationMediaFile?> GetByMediaFileAndStationAsync(Guid mediaFileId, Guid stationId, CancellationToken cancellationToken = default);
    Task AddAsync(StationMediaFile stationMediaFile, CancellationToken cancellationToken = default);
    Task DeleteAsync(StationMediaFile stationMediaFile, CancellationToken cancellationToken = default);
}
