using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.DTOs;
using AccountContentService.Application.Features.BlogPosts.Queries.GetPosts;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using AccountContentService.Domain.Enums;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Infrastructure.Repositories
{
    public class BlogPostRepository : IBlogPostRepository
    {
        private readonly AccountContentDbContext _context;

        public BlogPostRepository(AccountContentDbContext content)
        {
            _context = content;
        }
        public async Task AddAsync(BlogPost post)
        {
            await _context.BlogPosts.AddAsync(post);

            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(BlogPost post)
        {
            _context.BlogPosts.Update(post);
            await _context.SaveChangesAsync();
        }

        public async Task<PaginationResult<BlogPost>> GetAllPostsAsync(
    GetPostsQuery request,
    CancellationToken cancellationToken)
        {
            IQueryable<BlogPost> query = _context.BlogPosts
                .AsNoTracking();

            // FILTER STATUS
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                query = query.Where(p => p.Status.ToLower() == request.Status.ToLower());
            }

            // FILTER MOOD TAG
            if (!string.IsNullOrWhiteSpace(request.MoodTag))
            {
                var mood = request.MoodTag.Trim().ToLower();

                query = query.Where(p =>
                    p.MoodTag.ToLower() == mood);
            }

            //// FILTER AUTHOR
            //if (request.AuthorId.HasValue)
            //{
            //    query = query.Where(p =>
            //        p.UserId == request.AuthorId.Value);
            //}

            // SEARCH (title + content)
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var keyword = request.Search.Trim().ToLower();

                query = query.Where(p =>
                    p.Title.ToLower().Contains(keyword) ||
                    p.ContentText.ToLower().Contains(keyword));
            }

            // DATE FILTER
            if (request.FromDate.HasValue)
            {
                query = query.Where(p =>
                    p.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(p =>
                    p.CreatedAt <= request.ToDate.Value);
            }

            // TOTAL COUNT
            var totalCount = await query.CountAsync(cancellationToken);

            // PAGINATION
            var posts = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip(Math.Max(0, (request.Page - 1) * request.PageSize))
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

        public async Task<PaginationResult<BlogPost>> GetAllPublishedPostsAsync(
            GetPublisedPostsQuery request,
            CancellationToken cancellationToken)
        {
            IQueryable<BlogPost> query = _context.BlogPosts
                .AsNoTracking();

            // FILTER STATUS
            query = query.Where(p => p.Status.ToLower() == PostStatus.Published.ToString().ToLower());

            // FILTER MOOD TAG
            if (!string.IsNullOrWhiteSpace(request.MoodTag))
            {
                var mood = request.MoodTag.Trim().ToLower();

                query = query.Where(p =>
                    p.MoodTag.ToLower() == mood);
            }

            //// FILTER AUTHOR
            //if (request.AuthorId.HasValue)
            //{
            //    query = query.Where(p =>
            //        p.UserId == request.AuthorId.Value);
            //}

            // SEARCH (title + content)
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var keyword = request.Search.Trim().ToLower();

                query = query.Where(p =>
                    p.Title.ToLower().Contains(keyword) ||
                    p.ContentText.ToLower().Contains(keyword));
            }

            // DATE FILTER
            if (request.FromDate.HasValue)
            {
                query = query.Where(p =>
                    p.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(p =>
                    p.CreatedAt <= request.ToDate.Value);
            }

            // TOTAL COUNT
            var totalCount = await query.CountAsync(cancellationToken);

            // PAGINATION
            var posts = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip(Math.Max(0, (request.Page - 1) * request.PageSize))
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

        public Task<BlogPost> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var query = _context.BlogPosts
                .AsNoTracking()
                .Where(x => x.Id == id);

            return query.FirstOrDefaultAsync(cancellationToken);
        }

        public Task<BlogPost> GetPublishByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var query = _context.BlogPosts
                .AsNoTracking()
                .Where(x => x.Status.ToLower() == "published")
                .Where(x => x.Id == id);

            return query.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task DeleteAsync(BlogPost post)
        {
            _context.BlogPosts.Remove(post);
            await _context.SaveChangesAsync();
        }
    }
}
