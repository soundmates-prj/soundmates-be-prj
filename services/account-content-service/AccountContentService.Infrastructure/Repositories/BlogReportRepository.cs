using AccountContentService.Application.DTOs;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class BlogReportRepository : IBlogReportRepository
{
    private readonly AccountContentDbContext _context;

    public BlogReportRepository(AccountContentDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(BlogReport report, CancellationToken cancellationToken)
    {
        await _context.Set<BlogReport>().AddAsync(report, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(BlogReport report, CancellationToken cancellationToken)
    {
        _context.Set<BlogReport>().Update(report);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<BlogReport?> GetByUserAndPostAsync(
        Guid userId,
        Guid postId,
        CancellationToken cancellationToken)
    {
        return await _context.Set<BlogReport>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.ReporterUserId == userId &&
                r.BlogPostId == postId,
                cancellationToken);
    }

    public async Task<bool> HasUserReportedPostAsync(
        Guid postId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _context.Set<BlogReport>()
            .AnyAsync(r =>
                r.BlogPostId == postId &&
                r.ReporterUserId == userId,
                cancellationToken);
    }

    public async Task<IEnumerable<BlogReport>> GetReportsByPostIdAsync(
        Guid postId,
        CancellationToken cancellationToken)
    {
        return await _context.Set<BlogReport>()
            .AsNoTracking()
            .Where(r => r.BlogPostId == postId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ReportedPostDto>> GetReportedPostsAsync(
    CancellationToken cancellationToken)
    {
        return await _context.BlogPosts
            .AsNoTracking()
            .Select(p => new
            {
                Post = p,
                ReportCount = p.Reports.Count()
            })
            .Where(x => x.ReportCount > 0)
            .OrderByDescending(x => x.ReportCount)
            .Select(x => new ReportedPostDto
            {
                postDto = new PostDto
                {
                    Id = x.Post.Id,
                    UserId = x.Post.UserId,
                    Title = x.Post.Title,
                    ContentText = x.Post.ContentText,
                    UserFullName = x.Post.UserFullName,
                    UserAvatarUrl = x.Post.UserAvatarUrl,
                    AudioUrl = x.Post.AudioUrl,
                    ImageUrl = x.Post.ImageUrl,
                    IsActive = x.Post.IsActive,
                    PrivacyScope = x.Post.PrivacyScope,
                    MoodTag = x.Post.MoodTag,
                    Status = x.Post.Status,
                    IsGenerated = x.Post.IsGenerated,
                    CreatedAt = x.Post.CreatedAt,
                    UpdatedAt = x.Post.UpdatedAt,
                    PublishedAt = x.Post.PublishedAt,

                    ShareMusic = null
                },
                ReportCount = x.ReportCount,
                CreatedAt = x.Post.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
    public async Task DeleteReportsByPostIdAsync(Guid postId, CancellationToken cancellationToken)
    {
        var reports = await _context.Set<BlogReport>()
            .Where(r => r.BlogPostId == postId)
            .ToListAsync(cancellationToken);
            
        if (reports.Any())
        {
            _context.Set<BlogReport>().RemoveRange(reports);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
} 