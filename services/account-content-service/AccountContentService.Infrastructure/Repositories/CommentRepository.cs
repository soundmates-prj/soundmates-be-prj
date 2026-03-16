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


        // ================================
        // BASE QUERY BUILDER
        // ================================
        private IQueryable<BlogPost> BuildQuery(
            IQueryable<BlogPost> query,
            string? moodTag,
            string? search,
            DateTime? fromDate,
            DateTime? toDate)
        {
            if (!string.IsNullOrWhiteSpace(moodTag))
                query = query.Where(p => p.MoodTag == moodTag);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p =>
                    p.Title.Contains(search) ||
                    p.ContentText.Contains(search));

            if (fromDate.HasValue)
                query = query.Where(p => p.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(p => p.CreatedAt <= toDate.Value);

            return query;
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

        public async Task<PaginationResult<BlogPost>> GetAllPostsAsync(
            GetPostsQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.BlogPosts
                .AsNoTracking()
                .Include(x => x.Comments)
                .Include(x => x.Reactions)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Status))
                query = query.Where(p => p.Status == request.Status);

            query = BuildQuery(
                query,
                request.MoodTag,
                request.Search,
                request.FromDate,
                request.ToDate);

            var totalCount = await query.CountAsync(cancellationToken);

            var posts = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PaginationResult<BlogPost>
            {
                Items = posts,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
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

        public async Task UpdateAsync(BlogComment comment)
        {
            _context.BlogComments.Update(comment);
            await _context.SaveChangesAsync();
        }
    }
}
