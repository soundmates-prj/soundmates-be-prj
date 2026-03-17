using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Infrastructure.Repositories
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly AccountContentDbContext _context;
        public SubscriptionRepository(AccountContentDbContext context)
        {
            _context = context;
        }

        public async Task AddPlanAsync(SubscriptionPlan comment)
        {
            await _context.SubscriptionPlans.AddAsync(comment);
            await _context.SaveChangesAsync();
        }

        public Task DeletePlanAsync(SubscriptionPlan comment)
        {
            _context.SubscriptionPlans.Remove(comment);
            return _context.SaveChangesAsync();
        }

        public async Task<List<SubscriptionPlan>> GetAllPlansAsync(CancellationToken cancellationToken)
        {
            var query = _context.SubscriptionPlans
                .AsNoTracking()
                .AsQueryable();

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<SubscriptionPlan> GetPlanByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.SubscriptionPlans
                .AsNoTracking()
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public Task UpdatePlanAsync(SubscriptionPlan comment)
        {
            _context.SubscriptionPlans.Update(comment);
            return _context.SaveChangesAsync();
        }
    }
}
