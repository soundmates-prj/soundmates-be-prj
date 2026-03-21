using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using AccountContentService.Domain.Enums;
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

        public async Task AddAsync(Subscription comment)
        {
            await _context.Subscriptions.AddAsync(comment);
            await _context.SaveChangesAsync();
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

        public async Task<Subscription> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            var activeStatus = SubscriptionStatus.Active.ToString().ToLower();

            return await _context.Subscriptions
                .AsNoTracking()
                .Include(x => x.Plan)
                .Where(x => x.UserId == userId && x.Status.ToLower() == activeStatus)
                .OrderByDescending(x => x.SubscribeAt)
                .FirstOrDefaultAsync(cancellationToken);
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

        public async Task<Subscription> GetSubscriptionByIdAsync(Guid subId, CancellationToken cancellationToken)
        {
            return await _context.Subscriptions
                .AsNoTracking()
                .Where(x => x.Id == subId)
                .Include(x => x.Plan)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Subscription> GetSubscriptionByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await _context.Subscriptions
                .AsNoTracking()
                .OrderByDescending(x => x.SubscribeAt)
                .Where(x => x.UserId == userId)
                .Include(x => x.Plan)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<PaginationResult<Subscription>> GetSubscriptionsAsync(int page, int pageSize, CancellationToken cancellationToken)
        {
            var query = _context.Subscriptions
                .AsNoTracking()
                .Include(x => x.Plan)
                .AsQueryable();

            var totalCount = await query.CountAsync(cancellationToken);

            var subscriptions = await query
                .OrderByDescending(x => x.StartDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PaginationResult<Subscription>
            {
                Items = subscriptions,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PaginationResult<Subscription>> GetSubscriptionsHistoryAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken)
        {
            var query = _context.Subscriptions
                .AsNoTracking()
                .Include(x => x.Plan)
                .Where(x => x.UserId == userId)
                .AsQueryable();

            var totalCount = await query.CountAsync(cancellationToken);

            var subscriptions = await query
                .OrderByDescending(x => x.StartDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PaginationResult<Subscription>
            {
                Items = subscriptions,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public Task UpdatePlanAsync(SubscriptionPlan comment)
        {
            _context.SubscriptionPlans.Update(comment);
            return _context.SaveChangesAsync();
        }
    }
}
