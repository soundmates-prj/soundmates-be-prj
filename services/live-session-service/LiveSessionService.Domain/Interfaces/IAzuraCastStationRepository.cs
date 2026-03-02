using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Domain.Interfaces;

public interface IAzuraCastStationRepository
{
    Task<AzuraCastStation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AzuraCastStation?> GetByExternalIdAsync(int externalStationId, CancellationToken cancellationToken = default);
    Task<List<AzuraCastStation>> GetAllEnabledAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AzuraCastStation station, CancellationToken cancellationToken = default);
    Task UpdateAsync(AzuraCastStation station, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task SyncMountsAsync(Guid stationId, IEnumerable<StationMount> newMounts, CancellationToken cancellationToken = default);
}
