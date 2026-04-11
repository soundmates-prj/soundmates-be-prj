using AccountContentService.Application.Common.Pagination;
using AccountContentService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Interfaces.Repositories
{
    public interface ISubscriptionRepository
    {
        /// <summary>
        /// CRUD operations for subcription 
        ///</summary>
        Task AddAsync(Subscription comment);
        Task UpdateAsync(Subscription subscription);

        /// <summary>
        /// CRUD operations for subcription plans
        ///</summary>
        Task AddPlanAsync(SubscriptionPlan comment);
        Task UpdatePlanAsync(SubscriptionPlan comment);
        Task DeletePlanAsync(SubscriptionPlan comment);

        /// <summary>
        /// Query subscription
        ///</summary>
        Task<PaginationResult<Subscription>> GetSubscriptionsAsync(int page, int pageSize, CancellationToken cancellationToken);
        Task<PaginationResult<Subscription>> GetSubscriptionsHistoryAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken);

        Task<Subscription> GetSubscriptionByIdAsync(Guid subId, CancellationToken cancellationToken);
        Task<Subscription> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken);
        Task<Subscription> GetSubscriptionByUserIdAsync(Guid userId, CancellationToken cancellationToken);

        /// <summary>
        /// Query plans 
        ///</summary>
        Task<SubscriptionPlan> GetPlanByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<List<SubscriptionPlan>> GetAllPlansAsync(CancellationToken cancellationToken);
    }
}
