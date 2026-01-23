using AuthQueryService.Domain.Entities.ReadModels;

namespace AuthQueryService.Domain.Interfaces
{
    /// <summary>
    /// Repository interface for read-only role queries
    /// Part of Query Service - no mutations allowed
    /// </summary>
    public interface IRoleRepository
    {
        Task<RoleReadModel?> GetByIdAsync(Guid id);
        Task<RoleReadModel?> GetByNameAsync(string name);
        Task<List<RoleReadModel>> GetAllAsync();
    }
}
