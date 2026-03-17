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
        /// CRUD operations for comments
        ///</summary>
        Task AddAsync(PostReaction comment);
        Task UpdateAsync(PostReaction comment);
        Task DeleteAsync(PostReaction comment);

        /// <summary>
        /// Query comments by post ID, with pagination
        ///</summary>
        Task<PaginationResult<PostReaction>> GetByUserIdAsync(Guid userId, int pageSize, int page, CancellationToken cancellationToken);
        Task<PostReaction> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<List<PostReaction>> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken);

        Task<PaginationResult<PostReaction>> GetByPostIdAsync(Guid postId, int pageSize, int page, CancellationToken cancellationToken);
    }
}
