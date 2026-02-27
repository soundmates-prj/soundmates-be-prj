using Microsoft.EntityFrameworkCore;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using LiveSessionService.Infrastructure.Persistence;

namespace LiveSessionService.Infrastructure.Repositories;

public sealed class AzuraCastStationRepository : IAzuraCastStationRepository
{
    private readonly LiveSessionDbContext _context;

    public AzuraCastStationRepository(LiveSessionDbContext context)
    {
        _context = context;
    }

    public async Task<AzuraCastStation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AzuraCastStations
            .Include(x => x.Mounts)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<AzuraCastStation?> GetByExternalIdAsync(int externalStationId, CancellationToken cancellationToken = default)
    {
        return await _context.AzuraCastStations
            .FirstOrDefaultAsync(x => x.ExternalStationId == externalStationId, cancellationToken);
    }

    public async Task<List<AzuraCastStation>> GetAllEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AzuraCastStations
            .Include(x => x.Mounts)
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.ExternalStationId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AzuraCastStation station, CancellationToken cancellationToken = default)
    {
        await _context.AzuraCastStations.AddAsync(station, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AzuraCastStation station, CancellationToken cancellationToken = default)
    {
        _context.AzuraCastStations.Update(station);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var station = await GetByIdAsync(id, cancellationToken);
        if (station != null)
        {
            _context.AzuraCastStations.Remove(station);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SyncMountsAsync(Guid stationId, IEnumerable<StationMount> newMounts, CancellationToken cancellationToken = default)
    {
        var existingMounts = await _context.StationMounts
            .Where(m => m.AzuraCastStationId == stationId)
            .ToListAsync(cancellationToken);

        var newMountList = newMounts.ToList();
        var newExternalIds = newMountList.Select(m => m.ExternalMountId).ToHashSet();

        // Remove stale mounts
        var toRemove = existingMounts.Where(m => !newExternalIds.Contains(m.ExternalMountId)).ToList();
        if (toRemove.Count > 0)
            _context.StationMounts.RemoveRange(toRemove);

        // Add or update
        foreach (var mount in newMountList)
        {
            var existing = existingMounts.FirstOrDefault(m => m.ExternalMountId == mount.ExternalMountId);
            if (existing == null)
            {
                await _context.StationMounts.AddAsync(mount, cancellationToken);
            }
            else
            {
                existing.MountName = mount.MountName;
                existing.MountPath = mount.MountPath;
                existing.MountUrl = mount.MountUrl;
                existing.IsDefault = mount.IsDefault;
                existing.Bitrate = mount.Bitrate;
                existing.Format = mount.Format;
                existing.CurrentListeners = mount.CurrentListeners;
                existing.UniqueListeners = mount.UniqueListeners;
                existing.UpdatedAt = mount.UpdatedAt;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
