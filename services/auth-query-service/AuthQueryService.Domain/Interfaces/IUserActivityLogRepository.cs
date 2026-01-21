using AuthQueryService.Domain.Entities.ReadModels;

namespace AuthQueryService.Domain.Interfaces
{
    public interface IUserActivityLogRepository
    {
        Task CreateAsync(UserActivityLog log);
        Task<UserActivityLog?> GetByIdAsync(Guid id);
        Task<List<UserActivityLog>> GetByUserIdAsync(Guid userId, int limit = 50);
        Task<List<UserActivityLog>> GetRecentActivitiesAsync(int limit = 100);
        Task<List<UserActivityLog>> GetFailedLoginAttemptsAsync(string? emailOrUsername = null, int hours = 24);
        Task<(List<UserActivityLog> Items, int TotalCount)> GetPagedAsync(int page, int size, string? activityType = null, bool? isSuccess = null);
    }
}
