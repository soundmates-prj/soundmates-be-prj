using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.Features.BlogPosts.Queries.GetPopularPosts;
using AccountContentService.Application.Features.BlogPosts.Queries.GetPosts;
using AccountContentService.Application.Features.BlogPosts.Queries.GetPostStats;
using AccountContentService.Application.Features.BlogPosts.Queries.GetTrendingPosts;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using AccountContentService.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountContentService.Infrastructure.Repositories
{
    public class BlogPostRepository : IBlogPostRepository
    {
        private readonly AccountContentDbContext _context;

        public BlogPostRepository(AccountContentDbContext context)
        {
            _context = context;
        }

        // ================================
        // CREATE
        // ================================
        public async Task AddAsync(BlogPost post)
        {
            await _context.BlogPosts.AddAsync(post);
            await _context.SaveChangesAsync();
        }

        // ================================
        // UPDATE
        // ================================
        public async Task UpdateAsync(BlogPost post)
        {
            _context.BlogPosts.Update(post);
            await _context.SaveChangesAsync();
        }

        // ================================
        // DELETE
        // ================================
        public async Task DeleteAsync(BlogPost post)
        {
            _context.BlogPosts.Remove(post);
            await _context.SaveChangesAsync();
        }

        // ================================
        // GET BY ID
        // ================================
        public async Task<BlogPost> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.BlogPosts
                .AsNoTracking()
                .Include(x => x.Comments)
                .Include(x => x.Reactions)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<BlogPost> GetPublishByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.BlogPosts
                .AsNoTracking()
                .Include(x => x.Comments)
                .Include(x => x.Reactions)
                .Where(x => x.Status == PostStatus.Published.ToString())
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<PaginationResult<BlogPost>> GetByUserIdAsync(
             Guid userId,
            int pageSize, int page,
            CancellationToken cancellationToken)
        {
            var query = _context.BlogPosts
                .AsNoTracking()
                .Include(x => x.Comments)
                .Include(x => x.Reactions)
                .Where(x => x.UserId == userId)
                .AsQueryable();

            var totalCount = await query.CountAsync(cancellationToken);

            var posts = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PaginationResult<BlogPost>
            {
                Items = posts,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
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

        // ================================
        // GET ALL POSTS (ADMIN)
        // ================================
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

        // ================================
        // GET PUBLISHED POSTS (PUBLIC)
        // ================================
        public async Task<PaginationResult<BlogPost>> GetAllPublishedPostsAsync(
            GetPublisedPostsQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.BlogPosts
                .AsNoTracking()
                .Include(x => x.Comments)
                .Include(x => x.Reactions)
                .Where(p => p.Status == PostStatus.Published.ToString());

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

        // ================================
        // TRENDING POSTS
        // ================================
        public async Task<PaginationResult<TrendingPostResponse>> GetTrendingPostsAsync(
            GetTrendingPostsQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.BlogPosts
                .AsNoTracking()
                .Where(p => p.Status == PostStatus.Published.ToString());

            query = BuildQuery(
                query,
                request.MoodTag,
                request.Search,
                request.FromDate,
                request.ToDate);

            var totalCount = await query.CountAsync(cancellationToken);

            var posts = await query
                .Select(p => new TrendingPostResponse
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    Title = p.Title,
                    ContentText = p.ContentText,
                    AudioUrl = p.AudioUrl,
                    PrivacyScope = p.PrivacyScope,
                    MoodTag = p.MoodTag,
                    Status = p.Status,
                    IsGenerated = p.IsGenerated,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    PublishedAt = p.PublishedAt,
                    ReactionCount = p.Reactions.Count,
                    CommentCount = p.Comments.Count
                })
                .OrderByDescending(x => x.ReactionCount)
                .ThenByDescending(x => x.CommentCount)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PaginationResult<TrendingPostResponse>
            {
                Items = posts,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        // ================================
        // POPULAR POSTS
        // ================================
        public async Task<PaginationResult<PopularPostsResponse>> GetPopularPostsAsync(
            GetPopularPostsQuery request,
            CancellationToken cancellationToken)
        {
            var weekAgo = DateTime.UtcNow.AddDays(-7);

            var query = _context.BlogPosts
                .AsNoTracking()
                .Where(p =>
                    p.Status == PostStatus.Published.ToString() &&
                    p.CreatedAt >= weekAgo);

            query = BuildQuery(
                query,
                request.MoodTag,
                request.Search,
                request.FromDate,
                request.ToDate);

            var totalCount = await query.CountAsync(cancellationToken);

            var posts = await query
                .Select(p => new
                {
                    Post = p,
                    ReactionCount = p.Reactions.Count,
                    CommentCount = p.Comments.Count,
                    Score =
                        (p.Reactions.Count * 2) +
                        (p.Comments.Count * 3) -
                        (DateTime.UtcNow - p.CreatedAt).TotalHours
                })
                .OrderByDescending(x => x.Score)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new PopularPostsResponse
                {
                    Id = x.Post.Id,
                    UserId = x.Post.UserId,
                    Title = x.Post.Title,
                    ContentText = x.Post.ContentText,
                    AudioUrl = x.Post.AudioUrl,
                    PrivacyScope = x.Post.PrivacyScope,
                    MoodTag = x.Post.MoodTag,
                    Status = x.Post.Status,
                    IsGenerated = x.Post.IsGenerated,
                    CreatedAt = x.Post.CreatedAt,
                    UpdatedAt = x.Post.UpdatedAt,
                    PublishedAt = x.Post.PublishedAt,
                    ReactionCount = x.ReactionCount,
                    CommentCount = x.CommentCount
                })
                .ToListAsync(cancellationToken);

            return new PaginationResult<PopularPostsResponse>
            {
                Items = posts,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        // ================================
        // POST STATS LIST
        // ================================
        public async Task<PaginationResult<PostStatsResponse>> GetPostsStatsAsync(
            GetPostsStatsQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.BlogPosts
                .AsNoTracking()
                .Where(p => p.PublishedAt != null);

            query = BuildQuery(
                query,
                request.MoodTag,
                request.Search,
                request.FromDate,
                request.ToDate);

            var totalCount = await query.CountAsync(cancellationToken);

            var posts = await query
                .Select(p => new PostStatsResponse
                {
                    PostId = p.Id,
                    ReactionCount = p.Reactions.Count,
                    CommentCount = p.Comments.Count,
                    PublishedAt = p.PublishedAt
                })
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PaginationResult<PostStatsResponse>
            {
                Items = posts,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        // ================================
        // POST STATS SINGLE
        // ================================
        public async Task<PostStatsResponse> GetPostStatsAsync(Guid postId)
        {
            return await _context.BlogPosts
                .Where(p => p.Id == postId && p.PublishedAt != null)
                .AsNoTracking()
                .Select(p => new PostStatsResponse
                {
                    PostId = p.Id,
                    ReactionCount = p.Reactions.Count,
                    CommentCount = p.Comments.Count,
                    PublishedAt = p.PublishedAt
                })
                .FirstOrDefaultAsync();
        }
    }
}