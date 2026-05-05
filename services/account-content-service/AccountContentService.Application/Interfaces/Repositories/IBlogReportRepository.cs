using AccountContentService.Application.DTOs;
using AccountContentService.Domain.Entities;

namespace AccountContentService.Application.Interfaces.Repositories;

public interface IBlogReportRepository
{
    Task AddAsync(BlogReport report, CancellationToken cancellationToken);
    Task UpdateAsync(BlogReport report, CancellationToken cancellationToken);
    Task<BlogReport?> GetByUserAndPostAsync(Guid userId, Guid postId, CancellationToken cancellationToken);
    Task<IEnumerable<ReportedPostDto>> GetReportedPostsAsync(CancellationToken cancellationToken);
    Task<IEnumerable<BlogReport>> GetReportsByPostIdAsync(Guid postId, CancellationToken cancellationToken);
    Task<bool> HasUserReportedPostAsync(Guid postId, Guid userId, CancellationToken cancellationToken);
    Task DeleteReportsByPostIdAsync(Guid postId, CancellationToken cancellationToken);
}