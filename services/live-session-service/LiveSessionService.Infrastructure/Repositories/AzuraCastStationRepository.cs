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
}
