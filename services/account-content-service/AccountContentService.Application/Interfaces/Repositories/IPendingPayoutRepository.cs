using AccountContentService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AccountContentService.Application.Interfaces.Repositories;

public interface IPendingPayoutRepository
{
    Task AddAsync(PendingPayout payout);
    Task UpdateAsync(PendingPayout payout);
    Task<PendingPayout?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<PendingPayout>> GetPendingPayoutsAsync(DateTime maxScheduledAt, CancellationToken cancellationToken);
}
