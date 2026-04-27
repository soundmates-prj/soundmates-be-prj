using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.Features.BlogPosts.Queries.GetPosts;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Infrastructure.Repositories
{
    public class CommentRepository : ICommentRepository
    {
        private readonly AccountContentDbContext _context;

        public CommentRepository(AccountContentDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(BlogComment comment)
        {
            await _context.BlogComments.AddAsync(comment);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(BlogComment comment)
        {
            _context.BlogComments.Remove(comment);
            await _context.SaveChangesAsync();
        }

        public async Task<BlogComment> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.BlogComments
                .AsNoTracking()
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        

        public async Task<PaginationResult<BlogComment>> GetByUserIdAsync(
                 Guid userId,
                int pageSize, int page,
                CancellationToken cancellationToken)
        {
            var query = _context.BlogComments
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .AsQueryable();

            var totalCount = await query.CountAsync(cancellationToken);

            var posts = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PaginationResult<BlogComment>
            {
                Items = posts,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<BlogComment>> GetDetailByIdAsync(
                 Guid id,
                CancellationToken cancellationToken)
        {
            var query = _context.BlogComments
                .AsNoTracking()
                .Where(x => x.Id == id || x.ParentCommentId == id)
                .AsQueryable();

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<PaginationResult<BlogComment>> GetByPostIdAsync(Guid postId, 
            int pageSize, int page, CancellationToken cancellationToken)
        {
            var query = _context.BlogComments
                .AsNoTracking()
                .Where(x => x.PostId == postId)
                .AsQueryable();

            var totalCount = await query.CountAsync(cancellationToken);

            var posts = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PaginationResult<BlogComment>
            {
                Items = posts,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task UpdateAsync(BlogComment comment)
        {
            _context.BlogComments.Update(comment);
            await _context.SaveChangesAsync();
        }
    }
}
