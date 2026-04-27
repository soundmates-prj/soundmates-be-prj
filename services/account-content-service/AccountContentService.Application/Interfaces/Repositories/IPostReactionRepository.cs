using AccountContentService.Application.Common.Pagination;
using AccountContentService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Interfaces.Repositories
{
    public interface IPostReactionRepository
    {
        /// <summary>
        /// CRUD operations for reactions
        ///</summary>
        Task AddAsync(PostReaction reaction);
        Task UpdateAsync(PostReaction reaction);
        Task DeleteAsync(PostReaction reaction);

        /// <summary>
        /// Query reactions by post ID, with pagination
        ///</summary>
        Task<List<PostReaction>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
        Task<PostReaction> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<List<PostReaction>> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<PostReaction> GetUserReactionAsync(Guid userId, Guid postId, CancellationToken cancellationToken);
        Task<List<PostReaction>> GetByPostIdAsync(Guid postId, CancellationToken cancellationToken);
    }
}
