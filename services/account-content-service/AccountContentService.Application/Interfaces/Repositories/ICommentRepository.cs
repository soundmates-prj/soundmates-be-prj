using AccountContentService.Application.Common.Pagination;
using AccountContentService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Interfaces.Repositories
{
    public interface ICommentRepository
    {
        /// <summary>
        /// CRUD operations for comments
        ///</summary>
        Task AddAsync(BlogComment comment);
        Task UpdateAsync(BlogComment comment);
        Task DeleteAsync(BlogComment comment);

        /// <summary>
        /// Query comments by post ID, with pagination
        ///</summary>
        Task<PaginationResult<BlogComment>> GetByUserIdAsync(Guid userId, int pageSize, int page, CancellationToken cancellationToken);
        Task<BlogComment> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    }
}
