using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Features.BlogPosts.Queries.GetPosts;
using AccountContentService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Interfaces.Repositories
{
    public interface IBlogPostRepository
    {
        /// <summary>
        /// CRUD
        //</summary>
        Task AddAsync(BlogPost post);
        Task UpdateAsync(BlogPost post);
        Task DeleteAsync(BlogPost post);

        /// <summary>
        /// Query blog posts with pagination and optional filters
        /// </summary>
        Task<PaginationResult<BlogPost>> GetAllPostsAsync(GetPostsQuery request, CancellationToken cancellationToken);
        Task<PaginationResult<BlogPost>> GetAllPublishedPostsAsync(GetPublisedPostsQuery request, CancellationToken cancellationToken);
        Task<BlogPost> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<BlogPost> GetPublishByIdAsync(Guid id, CancellationToken cancellationToken);
    }
}
