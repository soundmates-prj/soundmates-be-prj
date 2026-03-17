using AccountContentService.Application.Common.Pagination;
using AccountContentService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Interfaces.Repositories
{
    public interface ISubscriptionRepository
    {
        ///// <summary>
        ///// CRUD operations for subcription 
        /////</summary>
        //Task AddAsync(Subscription comment);
        //Task UpdateAsync(Subscription comment);
        //Task DeleteAsync(Subscription comment);

        /// <summary>
        /// CRUD operations for subcription plans
        ///</summary>
        Task AddPlanAsync(SubscriptionPlan comment);
        Task UpdatePlanAsync(SubscriptionPlan comment);
        Task DeletePlanAsync(SubscriptionPlan comment);

        ///// <summary>
        ///// Query comments by post ID, with pagination
        /////</summary>
        //Task<PaginationResult<BlogComment>> GetByUserIdAsync(Guid userId, int pageSize, int page, CancellationToken cancellationToken);
        //Task<BlogComment> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        //Task<List<BlogComment>> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken);

        //Task<PaginationResult<BlogComment>> GetByPostIdAsync(Guid postId, int pageSize, int page, CancellationToken cancellationToken);

        /// <summary>
        /// Query plans 
        ///</summary>
        Task<SubscriptionPlan> GetPlanByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<List<SubscriptionPlan>> GetAllPlansAsync(CancellationToken cancellationToken);
    }
}
