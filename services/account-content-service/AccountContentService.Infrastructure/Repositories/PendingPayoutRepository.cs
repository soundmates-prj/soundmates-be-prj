using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using AccountContentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AccountContentService.Infrastructure.Repositories;

public class PendingPayoutRepository : IPendingPayoutRepository
{
    private readonly AccountContentDbContext _context;

    public PendingPayoutRepository(AccountContentDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PendingPayout payout)
    {
        await _context.PendingPayouts.AddAsync(payout);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PendingPayout payout)
    {
        _context.PendingPayouts.Update(payout);
        await _context.SaveChangesAsync();
    }

    public async Task<PendingPayout?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.PendingPayouts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<PendingPayout>> GetPendingPayoutsAsync(DateTime maxScheduledAt, CancellationToken cancellationToken)
    {
        return await _context.PendingPayouts
            .Where(x => x.Status == "pending" && x.ScheduledAt <= maxScheduledAt)
            .ToListAsync(cancellationToken);
    }
}
